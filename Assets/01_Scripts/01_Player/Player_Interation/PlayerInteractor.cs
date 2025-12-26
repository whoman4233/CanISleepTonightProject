using System;
using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
{
    private const float ViewportCenterX = 0.5f;
    private const float ViewportCenterY = 0.5f;

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Ray Settings")]
    [SerializeField] private float interactDistance = 1f;
    [SerializeField] private LayerMask interactLayerMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool drawInteractAttemptRay = true;
    [SerializeField] private bool drawDebugRay = true;

    private Player _player;
    private InteractableOutliner _currentOutliner;

    [SerializeField] private float interactSphereRadius = 0.15f; // ✅ SphereCast 두께(반지름)

    // UI/다른 시스템이 읽을 수 있는 "현재 타겟" 정보
    public bool HasTarget => _currentInteractable != null;
    public GameObject CurrentTargetObject => _currentHitCollider ? _currentHitCollider.gameObject : null;
    public float CurrentTargetDistance => _currentHitDistance;

    private IInteractable _currentInteractable;
    private Collider _currentHitCollider;
    private float _currentHitDistance;

    // =========================
    // Crosshair Hover 이벤트/상태 제어
    // =========================
    private bool _lastHoverState;
    private bool _inspectionActive;

    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;

    private void Awake()
    {
        _player = GetComponent<Player>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (_player == null)
        {
            Debug.LogError("[PlayerInteractor] Player 컴포넌트를 찾지 못했습니다.");
            enabled = false;
        }

        if (targetCamera == null)
        {
            Debug.LogError("[PlayerInteractor] Camera가 비어있습니다. Inspector에 할당하거나 MainCamera 태그를 확인하세요.");
            enabled = false;
        }

        // =========================
        // [ADDED] 이벤트 핸들러 캐싱 (람다 unsubscribe 문제 방지)
        // =========================
        _onInspectionStarted = _ =>
        {
            _inspectionActive = true;
            ForceClearScanAndPublishOff();
        };

        _onInspectionEnded = _ =>
        {
            _inspectionActive = false;
            ForceClearScanAndPublishOff(); // 재진입 시 잔상 방지
        };

    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onInspectionStarted);
        EventBus.Subscribe(_onInspectionEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onInspectionStarted);
        EventBus.Unsubscribe(_onInspectionEnded);

        // 비활성화 시 잔상 정리
        ForceClearScanAndPublishOff();
    }

    private void Update()
    {

        // Inspection 중이면 스캔 금지
        if (_inspectionActive)
        {
            ForceClearScanAndPublishOff();
            return;
        }

        // UIOnly 상태면 스캔 금지 (InputManager)
        if (InputManager.Instance != null && InputManager.Instance.CurrentState == InputState.UIOnly)
        {
            ForceClearScanAndPublishOff();
            return;
        }

        // 상시 스캔(감지)
        Scan();
    }

    /// <summary>
    /// 매 프레임: 화면 중앙으로 SphereCast를 쏴서 현재 Interactable을 캐싱
    /// </summary>
    private void Scan()
    {
        _currentInteractable = null;
        _currentHitCollider = null;
        _currentHitDistance = 0f;

        Ray ray = targetCamera.ViewportPointToRay(new Vector3(ViewportCenterX, ViewportCenterY, 0f));

        if (drawDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green, 0f);

        // ✅ Physics.Raycast를 Physics.SphereCast로 변경하여 조준 판정 강화
        if (!Physics.SphereCast(ray, interactSphereRadius, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (_currentOutliner != null)
            {
                _currentOutliner.SetHighlight(false);
                _currentOutliner = null;
            }
            PublishHoverIfChanged(false); // 타겟없을 때(맞추지 않았을 때 이벤트 발행해야 크로스헤어에 이상없음)
            return;
        }

        _currentHitCollider = hit.collider;
        _currentHitDistance = hit.distance;

        // 상호작용 인터페이스 탐색 (본인 혹은 부모)
        _currentInteractable = hit.collider.GetComponentInParent<IInteractable>();

        // 아웃라이너 찾기 (본인 혹은 부모)
        InteractableOutliner nextOutliner = hit.collider.GetComponentInParent<InteractableOutliner>();

        if (_currentOutliner != nextOutliner)
        {
            if (_currentOutliner != null)
                _currentOutliner.SetHighlight(false);

            _currentOutliner = nextOutliner;

            if (_currentOutliner != null)
                _currentOutliner.SetHighlight(true);
        }

        // =========================
        // 타겟 유무 변화 시 Hover 이벤트 발행
        // =========================

        PublishHoverIfChanged(_currentInteractable != null);
    }

    /// <summary>
    /// E키 눌렀을 때만 호출: 현재 캐싱된 대상이 있으면 상호작용 실행
    /// </summary>
    public bool TryInteract()
    {
        if (drawDebugRay || drawInteractAttemptRay)
        {
            Ray ray = targetCamera.ViewportPointToRay(new Vector3(ViewportCenterX, ViewportCenterY, 0f));
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 0.15f); // 0.15초만 보이게
        }

        if (_currentInteractable == null)
            return false;

        _currentInteractable.Interact(_player);
        return true;
    }

    private void PublishHoverIfChanged(bool nowHasTarget)
    {
        if (_lastHoverState == nowHasTarget)
            return;

        _lastHoverState = nowHasTarget;
        EventBus.Publish(new InteractableHoverChangedEvent(nowHasTarget));
    }

    // =========================
    // Hover 이벤트 발행
    // =========================
    private void ForceClearScanAndPublishOff()
    {
        // Outliner 끄기
        if (_currentOutliner != null)
        {
            _currentOutliner.SetHighlight(false);
            _currentOutliner = null;
        }

        // 캐시 비우기
        _currentInteractable = null;
        _currentHitCollider = null;
        _currentHitDistance = 0f;

        // Hover가 켜져 있었다면 false 발행
        if (_lastHoverState)
        {
            _lastHoverState = false;
            EventBus.Publish(new InteractableHoverChangedEvent(false));
        }
    }
}