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

    private Action<InspectionViewReadyEvent> _onViewReady; // view 전용 이벤트

    private InteractableOutliner _currentOutlined; //아웃라인용

    private RectTransform inspectionViewRect;

    private PlayerInputs _inputs;

    private bool isInspecting;
    private float yaw;
    private float pitch;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    private void Awake()
    {
        _inputs = GetComponentInParent<Player>().Inputs;
        inspectionCamera.gameObject.SetActive(false);
        _onViewReady = OnViewReady;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onViewReady);
    }

    private void OnDisable()
    {
        Debug.Log("[InspectionManager] Unsubscribe ViewReady");
        EventBus.Unsubscribe(_onViewReady);
    }
    public void Initialize(PlayerInputs inputs)
    {
        _inputs = inputs;
    }
    private void Update()
    {
        if (!isInspecting || _inputs == null)
            return;

        if (_inputs.Inspection.Exit.WasPressedThisFrame())
        {
            ExitInspection();
            return;
        }

        HandleHoverOutline(); //아웃라인
        HandleRotation();
        HandleInspectClick();
    }

    // =========================
    // Inspection Lifecycle
    // =========================

    public void EnterInspection(IInspectable inspectable)
    {
        if (inspectable == null)
            return;

        isInspecting = true;
        currentInspectable = inspectable;

        _inputs.Player.Disable();
        _inputs.Inspection.Enable();

        inspectionCamera.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ResetRotation();
        SpawnInspectObject(inspectable.GetInspectPrefab());

        StartCoroutine(RequestViewNextFrame());
    }

    private IEnumerator RequestViewNextFrame()
    {
        yield return null;
        EventBus.Publish(new InspectionViewRequestedEvent());
    }

    public void ExitInspection()
    {
        isInspecting = false;

        if (inspectInstance != null)
        {
            Destroy(inspectInstance);
            inspectInstance = null;
        }

        _inputs.Inspection.Disable();
        _inputs.Player.Enable();

        currentInspectable = null;

        inspectionCamera.gameObject.SetActive(false);

        inspectionViewRect = null;
        EventBus.Publish(new InspectionViewReleasedEvent());

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        Debug.Log($"[InspectionManager] ViewRect assigned = {inspectionViewRect}");
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
    // Inspect Click (Ray)
    // =========================

    private void HandleInspectClick()
    {
        // 1. Inspection View 준비 여부
        if (inspectionViewRect == null)
            return;

        // 2. 클릭 입력 확인
        if (!_inputs.Inspection.InspectClick.WasPressedThisFrame())
            return;

        // 3. 마우스 위치가 Inspection 영역 안인지 확인
        Vector2 screenPos = Mouse.current.position.ReadValue();

        if (!RectTransformUtility.RectangleContainsScreenPoint(
                inspectionViewRect,
                screenPos,
                null))
            return;

        // 4. Viewport 좌표 계산
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

        // 5. Inspection Camera 기준 Ray 생성
        Ray ray = inspectionCamera.ViewportPointToRay(
            new Vector3(u, v, 0f)
        );

        Debug.DrawRay(ray.origin, ray.direction * inspectRayDistance, Color.green, 1.5f);

        // 6. Raycast
        if (!Physics.Raycast(ray, out RaycastHit hit, inspectRayDistance, inspectLayerMask))
        {
            Debug.Log("[InspectClick] Raycast 실패");
            return;
        }
        Debug.Log($"[InspectClick] Hit: {hit.collider.name}");
        // 7. InspectTarget 처리
        if (!hit.collider.TryGetComponent<IInspectTarget>(out var target))
        {
            Debug.Log($"[InspectClick] IInspectTarget 없음: {hit.collider.name}");
            return;
        }
        Debug.Log($"[InspectClick] IInspectTarget 발견: {hit.collider.name}");
        // 8. 실제 Inspect 실행
        target.OnInspect(currentInspectable);

        // 9. UX 정리 (클릭 성공 시에만)
        ClearOutline();
    }

    // =========================
    // Inspect Outline
    // =========================
    private void HandleHoverOutline()
    {
        if (inspectionViewRect == null)
        {
            Debug.Log("[HoverOutline] inspectionViewRect == null");
            return;
        }

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

        Debug.DrawRay(ray.origin, ray.direction * inspectRayDistance, Color.cyan, 0f);

        if (Physics.Raycast(ray, out var hit, inspectRayDistance, inspectLayerMask))
        {
            var outliner = hit.collider.GetComponentInChildren<InteractableOutliner>();
            Debug.Log($"[Hover] Hit={hit.collider.name}, Outliner={(outliner != null ? outliner.name : "NULL")}");
            if (outliner != null)
            {
                if (_currentOutlined != outliner)
                {
                    ClearOutline();
                    _currentOutlined = outliner;
                    _currentOutlined.SetHighlight(true);
                }
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






