using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InspectionManager : MonoBehaviour
{
    [Header("Camera/Pivot")]
    [SerializeField] private Camera inspectionCamera;
    [SerializeField] private Transform inspectPivot;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 0.15f;
    [SerializeField] private float pitchLimit = 80f;

    [Header("Ray")]
    [SerializeField] private LayerMask inspectLayerMask;
    [SerializeField] private float inspectRayDistance = 5f;
    [SerializeField] private float inspectHoverRadius = 0.08f;

    // =========================
    // 이벤트 핸들러 캐시
    // =========================
    private Action<InspectionViewReadyEvent> _onViewReady;
    private Action<InspectionRequestedEvent> _onInspectionRequested;
    private Action<ForceExitInspectionEvent> _onForceExit; //QTE 시작시 강제종료

    private InteractableOutliner _currentOutlined;
    private RectTransform inspectionViewRect;

    // =========================
    // Input / State
    // =========================
    private PlayerInputs _inputs;                     // 외부 주입만 받음
    private bool _initialized; // 초기화 완료 여부
    public bool IsInspecting => _isInspecting;
    private bool _isInspecting;

    private float yaw;
    private float pitch;

    private IInspectable currentInspectable;
    private GameObject inspectInstance;

    private Transform visualRoot; // 회전/스케일 조작 대상

    private void Awake()
    {
        inspectionCamera.gameObject.SetActive(false);

        _onViewReady = OnViewReady;
        _onInspectionRequested = OnInspectionRequested;
        _onForceExit = OnForceExitInspection;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onViewReady);
        EventBus.Subscribe(_onInspectionRequested);
        EventBus.Subscribe(_onForceExit);
    }

    private void OnDisable()
    {
        if (_isInspecting)
        {
            _isInspecting = false;

            EventBus.Publish(new InspectionEndedEvent());
            EventBus.Publish(new InspectionViewReleasedEvent());
        }

        EventBus.Unsubscribe(_onViewReady);
        EventBus.Unsubscribe(_onInspectionRequested);
        EventBus.Unsubscribe(_onForceExit);
    }

    // =========================
    // Initialization
    // =========================

    // Player에서 Inputs 주입
    public void Initialize(PlayerInputs inputs)
    {
        if (inputs == null)
        {
            Debug.LogError("[InspectionManager] Initialize failed: inputs is null");
            return;
        }

        _inputs = inputs;
        _initialized = true;

        Debug.Log("[InspectionManager] Initialized");
    }

    private void Update()
    {
        if (!_isInspecting)
            return;

        if (!_initialized || _inputs == null)
        {
            Debug.LogError("[InspectionManager] Update called while not initialized");
            return;
        }

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
        // 이벤트 외 직접 호출 대비 안전장치
        if (!_initialized)
        {
            Debug.LogWarning("[InspectionManager] EnterInspection called before initialization");
            return;
        }

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
    private void OnInspectionRequested(InspectionRequestedEvent e)
    {
        // 초기화 이전 요청 차단
        if (!_initialized)
        {
            Debug.LogWarning("[InspectionManager] Inspection requested before initialization");
            return;
        }

        EnterInspection(e.Target);
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
        }

        // =========================
        // VisualRoot 정리
        // =========================
        visualRoot = null;

        currentInspectable?.OnInspectionExit();
        currentInspectable = null;

        inspectionCamera.gameObject.SetActive(false);
        inspectionViewRect = null;

        EventBus.Publish(new InspectionEndedEvent());
        EventBus.Publish(new InspectionViewReleasedEvent());
    }
    private void OnForceExitInspection(ForceExitInspectionEvent e) // QTE 강제종료
    {
        if (!IsInspecting)
            return;

        ExitInspection();
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

        yaw -= delta.x * rotateSpeed;
        pitch += delta.y * rotateSpeed;
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

        // UI 영역 밖이면 즉시 해제
        if (!RectTransformUtility.RectangleContainsScreenPoint(
                inspectionViewRect,
                screenPos,
                null))
        {
            ClearOutline();
            return;
        }

        // UI → Viewport 좌표 변환
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

        // SphereCast
        if (Physics.SphereCast(
                ray,
                inspectHoverRadius,
                out RaycastHit hit,
                inspectRayDistance,
                inspectLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            var nextOutliner = hit.collider.GetComponent<InteractableOutliner>();

            //대상이 바뀌었을 때만 토글
            if (_currentOutlined != nextOutliner)
            {
                if (_currentOutlined != null)
                    _currentOutlined.SetHighlight(false);

                _currentOutlined = nextOutliner;

                if (_currentOutlined != null)
                    _currentOutlined.SetHighlight(true);
            }

            // 히트 유지 중이면 Clear하지 않음
            return;
        }

        // SphereCast 실패했을 때만 해제
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

        // =========================
        // VisualRoot 탐색
        // =========================
        visualRoot = inspectInstance.transform.Find("VisualRoot");
        if (visualRoot == null)
        {
            visualRoot = inspectInstance.transform;
        }

        if (inspectInstance.TryGetComponent<IInspectionView>(out var view))
        {
            view.Bind(currentInspectable);
        }
    }
}








