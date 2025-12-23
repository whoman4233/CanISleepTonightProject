using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InspectionManager : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera inspectionCamera;
    [SerializeField] private Transform inspectPivot;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 0.15f;
    [SerializeField] private float pitchLimit = 80f;

    [Header("Ray")]
    [SerializeField] private LayerMask inspectLayerMask;
    [SerializeField] private float inspectRayDistance = 5f;

    private Action<InspectionViewReadyEvent> _onViewReady;

    private InteractableOutliner _currentOutlined;
    private RectTransform inspectionViewRect;

    private PlayerInputs _inputs;                     // 외부 주입만 받음

    public bool IsInspecting => _isInspecting;
    private bool _isInspecting;

    private float yaw;
    private float pitch;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    private void Awake()
    {
        inspectionCamera.gameObject.SetActive(false);

        _onViewReady = OnViewReady;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onViewReady);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onViewReady);
    }

    // =========================
    // Initialization
    // =========================

    // Player에서 Inputs 주입
    public void Initialize(PlayerInputs inputs)
    {
        _inputs = inputs;
    }

    private void Update()
    {
        if (!_isInspecting || _inputs == null)
            return;

        // Exit 입력은 소비만 함 (Enable/Disable X)
        if (_inputs.Inspection.Exit.WasPressedThisFrame())
        {
            ExitInspection();
            return;
        }

        HandleHoverOutline();
        HandleRotation();
        HandleInspectClick();
    }

    // =========================
    // Inspection Lifecycle
    // =========================

    public void EnterInspection(IInspectable inspectable)
    {
        if (inspectable == null || _isInspecting)
            return;

        _isInspecting = true;

        currentInspectable = inspectable;
        currentInspectable.OnInspectionEnter();

        inspectionCamera.gameObject.SetActive(true);

        ResetRotation();
        SpawnInspectObject(inspectable.GetInspectPrefab());

        // 입력/커서 직접 제어 제거
        // 상태만 알림
        EventBus.Publish(new InspectionStartedEvent { Target = inspectable });

        StartCoroutine(RequestViewNextFrame());
    }

    private IEnumerator RequestViewNextFrame()
    {
        yield return null;
        EventBus.Publish(new InspectionViewRequestedEvent());
    }

    public void ExitInspection()
    {
        if (!_isInspecting)
            return;

        _isInspecting = false;

        if (inspectInstance != null)
        {
            Destroy(inspectInstance);
            inspectInstance = null;
            currentInspectable.OnInspectionExit();
        }

        inspectionCamera.gameObject.SetActive(false);
        inspectionViewRect = null;

        // 입력/커서 복구 제거
        // 상태 종료 알림
        EventBus.Publish(new InspectionEndedEvent());
        EventBus.Publish(new InspectionViewReleasedEvent());

        currentInspectable = null;
    }

    // =========================
    // View Binding
    // =========================

    private void OnViewReady(InspectionViewReadyEvent e)
    {
        var ui = FindObjectOfType<InspectionUIController>();
        if (ui == null)
        {
            Debug.LogError("[InspectionManager] InspectionUIController not found");
            return;
        }

        inspectionViewRect = ui.GetInspectionViewRect();
    }

    // =========================
    // Rotation
    // =========================

    private void HandleRotation()
    {
        if (!_inputs.Inspection.RotateHold.IsPressed())
            return;

        Vector2 delta = _inputs.Inspection.Rotate.ReadValue<Vector2>();
        if (delta.sqrMagnitude < 0.001f)
            return;

        yaw += delta.x * rotateSpeed;
        pitch -= delta.y * rotateSpeed;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);

        inspectPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void ResetRotation()
    {
        yaw = 0f;
        pitch = 0f;
        inspectPivot.localRotation = Quaternion.identity;
    }

    // =========================
    // Inspect Click
    // =========================

    private void HandleInspectClick()
    {
        if (inspectionViewRect == null)
            return;

        if (!_inputs.Inspection.InspectClick.WasPressedThisFrame())
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                inspectionViewRect,
                screenPos,
                null))
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inspectionViewRect,
            screenPos,
            null,
            out Vector2 localPoint);

        Rect rect = inspectionViewRect.rect;

        float u = (localPoint.x - rect.xMin) / rect.width;
        float v = (localPoint.y - rect.yMin) / rect.height;

        if (u < 0f || u > 1f || v < 0f || v > 1f)
            return;

        Ray ray = inspectionCamera.ViewportPointToRay(new Vector3(u, v, 0f));

        if (!Physics.Raycast(ray, out RaycastHit hit, inspectRayDistance, inspectLayerMask))
            return;

        if (!hit.collider.TryGetComponent<IInspectTarget>(out var target))
            return;

        target.OnInspect(currentInspectable);
        ClearOutline();
    }

    // =========================
    // Inspect Outline
    // =========================

    private void HandleHoverOutline()
    {
        if (inspectionViewRect == null)
            return;

        Vector2 screenPos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                inspectionViewRect,
                screenPos,
                null))
        {
            ClearOutline();
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inspectionViewRect,
            screenPos,
            null,
            out Vector2 localPoint);

        Rect rect = inspectionViewRect.rect;

        float u = (localPoint.x - rect.xMin) / rect.width;
        float v = (localPoint.y - rect.yMin) / rect.height;

        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            ClearOutline();
            return;
        }

        Ray ray = inspectionCamera.ViewportPointToRay(new Vector3(u, v, 0f));

        if (Physics.Raycast(ray, out var hit, inspectRayDistance, inspectLayerMask))
        {
            var outliner = hit.collider.GetComponentInChildren<InteractableOutliner>();
            if (outliner != null && _currentOutlined != outliner)
            {
                ClearOutline();
                _currentOutlined = outliner;
                _currentOutlined.SetHighlight(true);
                return;
            }
        }

        ClearOutline();
    }

    private void ClearOutline()
    {
        if (_currentOutlined != null)
        {
            _currentOutlined.SetHighlight(false);
            _currentOutlined = null;
        }
    }

    // =========================
    // Spawn
    // =========================

    private void SpawnInspectObject(GameObject prefab)
    {
        if (prefab == null)
            return;

        inspectInstance = Instantiate(prefab, inspectPivot);
        inspectInstance.transform.localPosition = Vector3.zero;
        inspectInstance.transform.localRotation = Quaternion.identity;
        inspectInstance.transform.localScale = Vector3.one;

        if (inspectInstance.TryGetComponent<IInspectionView>(out var view))
        {
            view.Bind(currentInspectable);
        }
    }
}








