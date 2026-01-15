using System;
using UnityEngine;

public sealed class PlayerInteractor : MonoBehaviour
{

    [Header("Carry Position")]
    [SerializeField] private Transform carryParent; // 물체가 붙을 위치
    private ICarryable _heldItem; // 들고 있는 물체
    public ICarryable CurrentHeldItem => _heldItem;
    public bool IsCarrying => _heldItem != null; // helditem != null 이면 true
    public Transform CarryParent => carryParent; // 읽기전용

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
    private DialogueManager _dialogueManager;

    // =========================
    // Crosshair Hover 이벤트/상태 제어
    // =========================
    private bool _lastHoverState;
    private bool _inspectionActive;

    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;

    // =========================
    // Prompt 텍스트 출력 용
    // =========================
    private string _lastPromptId;

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

    private void Start()
    {
        _dialogueManager = FindObjectOfType<DialogueManager>();
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

        if (InputManager.Instance != null && InputManager.Instance.CurrentState == InputState.Dialogue) // 대화중이면 스캔 금지
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
        // 들고 있는 동안에는 Ray 기반 Prompt/Scan 무시
        if (IsCarrying)
        {
            PublishHoverIfChanged(false);
            PublishPromptIfChanged();
            return;
        }
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
            // Ray 타겟 캐시 비우기
            _currentInteractable = null;
            _currentHitCollider = null;
            _currentHitDistance = 0f;

            PublishHoverIfChanged(false);

            //Ray가 없어도 Carry 프롬프트는 계속 떠야 함
            PublishPromptIfChanged();
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
        PublishPromptIfChanged();
    }
    private void PublishPromptIfChanged()
    {
        string nextPromptId = null;

        // =========================
        // 1. Carry 프롬프트 (우선)
        // =========================
        var carryProvider = GetComponent<CarryPromptProvider>();
        if (carryProvider != null &&
            carryProvider.TryGetPromptId(
                PromptContext.Interact,
                out var carryPromptId))
        {
            nextPromptId = carryPromptId;
        }
        else
        {
            // =========================
            // 2. Ray 기반 프롬프트
            // =========================
            if (_currentHitCollider != null)
            {
                var provider =
                    _currentHitCollider.GetComponentInParent<IPromptProvider>();

                if (provider != null &&
                    provider.TryGetPromptId(
                        PromptContext.Interact,
                        out var id))
                {
                    nextPromptId = id;
                }
            }
        }

        if (_lastPromptId == nextPromptId)
            return;

        _lastPromptId = nextPromptId;

        EventBus.Publish(new PromptChangedEvent
        {
            context = PromptContext.Interact,
            promptId = nextPromptId
        });
    }


    /// <summary>
    /// E키 눌렀을 때만 호출: 현재 캐싱된 대상이 있으면 상호작용 실행
    /// </summary>
    public bool TryInteract()
    {

        if (InputManager.Instance != null && InputManager.Instance.CurrentState == InputState.Dialogue) // e키 눌렀을 때 대화중이면 다른상호작용 무시하고 대화 진행
        {
            if (_dialogueManager != null)
            {
                _dialogueManager.OnContinueClicked();
                return true; // 대화 넘기기에 성공했으므로 true 반환
            }
            return false;
        }

        if (drawDebugRay || drawInteractAttemptRay)
        {
            Ray ray = targetCamera.ViewportPointToRay(new Vector3(ViewportCenterX, ViewportCenterY, 0f));
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.red, 0.15f); // 0.15초만 보이게
        }

        if (IsCarrying) // 들고있나?
        {
            DropHeldItem(); // 내려놔
            return true;
        }

        if (_currentInteractable == null)
            return false;

        _currentInteractable.Interact(_player);
        return true;
    }
    public void SetHeldItem(ICarryable item)// 물체를 손에 드는 함수, 물체 들고있음을 인지시켜줌, helditem에 item 넣어준다.
    {
        _heldItem = item;
        RefreshPrompt();
    }
    public void ClearHeldItem() // 물체 비우는 함수, 물체 drop시 호출
    {
        _heldItem = null;
        RefreshPrompt();
    }

    // 호출순서 TryInteract - IsCarrying = false && ICarryable이면 들기, IsCarrying = true면 내려놓기 그 외는 기존과 동일
    public void DropHeldItem()
    {
        if (_heldItem != null)
        {
            _heldItem.Drop(_player); // 물체에게 명령
        }
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
        //  프롬프트 제거
        if (!string.IsNullOrEmpty(_lastPromptId))
        {
            _lastPromptId = null;
            EventBus.Publish(new PromptChangedEvent
            {
                context = PromptContext.Interact,
                promptId = null
            });
        }
    }
    public void RefreshPrompt()
    {
        PublishPromptIfChanged();
    }
}