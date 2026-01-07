using UnityEngine;
using UnityEngine.UI;

public sealed class CellDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip("비어있으면 일반 문(단순 개폐)으로 동작합니다.")]
    [SerializeField] private string cellId;

    [SerializeField] private Collider cellInsideTrigger; // ✅ 감방 내부를 덮는 트리거 콜라이더 (옵션)

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry; // ✅ 죄수를 찾기 위해 필요
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [Header("Settings")]
    [SerializeField] private float interactCooldown = 0.8f; // 문 여닫는 쿨타임 (애니메이션 길이와 비슷하게 설정)
    private float _lastInteractTime = -999f; // 마지막 상호작용 시간
    [SerializeField] private bool useRedOutlineOnCloseOnlySlidingDoor = false;
    [SerializeField] private InteractableOutliner outliner;

    // [상태]
    [SerializeField] private bool _isPlayerInside;
    private bool _isSimpleDoorOpen = false;

    private static readonly int OpenHash = Animator.StringToHash("Open");
    private static readonly int CloseHash = Animator.StringToHash("Close");
    private static readonly int LockedHash = Animator.StringToHash("Locked");

    private void Awake()
    {
        if (outliner == null)
            outliner = GetComponentInChildren<InteractableOutliner>(true);

        if (outliner == null)
            outliner = GetComponent<InteractableOutliner>();
    }

    // ★ [추가] 이벤트 구독 (강제 개방 요청 수신)
    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            PrisonerEventBus.OnForceOpenDoor += HandleForceOpen;
        }
    }

    // ★ [추가] 이벤트 해지
    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            PrisonerEventBus.OnForceOpenDoor -= HandleForceOpen;
        }
    }

    // ★ [추가] 강제 개방 핸들러
    private void HandleForceOpen(string targetCellId)
    {
        // 내 방 번호가 아니면 무시
        if (this.cellId != targetCellId) return;

        if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방 요청 수신 (Ambush)");

        // 쿨타임이나 플레이어 상호작용 로직을 무시하고 즉시 문을 엽니다.
        // 기습 상황이므로 InspectionStateMachine은 건드리지 않고 시각적인 개방만 수행합니다.

        // 1. 단순 문 열기 애니메이션 재생
        PlayOpen();

        // 2. 필요하다면 상태 플래그 갱신 (단순 문으로 취급)
        // _isSimpleDoorOpen = true; 
    }

    public void Interact(Player player)
    {
        if (!Validate()) return;

        // [수정] 애니메이션 상태 확인 대신 쿨타임 체크로 변경
        // 이렇게 하면 애니메이션이 조금 꼬여도 일정 시간 지나면 무조건 다시 상호작용 가능
        if (Time.time < _lastInteractTime + interactCooldown)
        {
            if (verboseLog) Debug.Log($"[Door] 쿨타임 중... ({_lastInteractTime + interactCooldown - Time.time:F1}초 남음)");
            return;
        }

        // 쿨타임 갱신
        _lastInteractTime = Time.time;

        // 1. 일반 문 로직
        if (string.IsNullOrWhiteSpace(cellId))
        {
            HandleSimpleDoor();
            return;
        }

        // 2. 감방 전용 로직
        HandlePrisonDoor();
    }

    private void HandleSimpleDoor()
    {
        if (!_isSimpleDoorOpen) { PlayOpen(); _isSimpleDoorOpen = true; }
        else { PlayClose(); _isSimpleDoorOpen = false; }
    }

    private void HandlePrisonDoor()
    {
        // inspection이 null인지 안전장치
        if (inspection == null)
        {
            Debug.LogError($"[Door] {name}: InspectionStateMachine이 연결되지 않았습니다!");
            return;
        }

        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell)
        {
            // [문 열기] 점검 시작
            // [수정 2] TryEnter 실패 원인 파악을 위한 로그 추가
            if (TryEnter())
            {
                if (verboseLog) Debug.Log($"[Door] {cellId}: 문 열기 성공 & 점검 시작");
                //CellID 전달 이벤트 발행 -> 문열림 경고 HUD ON
                EventBus.Publish(new CellInspectionInProgressEvent
                {
                    CellId = cellId
                });
                PlayOpen();
                TriggerPrisonerInspection();
            }
            else
            {
                Debug.LogWarning($"[Door] {cellId}: 진입 불가 (TryEnter 실패). 잠김 애니메이션 재생.");
                PlayLocked();
            }
        }
        else
        {
            // ... (기존 닫기 로직 유지)
            if (cellInsideTrigger != null && _isPlayerInside)
            {
                Debug.LogWarning($"[Door] {cellId} 내부에 플레이어가 있어 문을 닫을 수 없습니다.");
                return;
            }

            if (TryExit())
            {
                PlayClose();
                inspection.CompleteInspection(cellId, inspection.IsSuppressionCleared);
                //CellID 전달 이벤트 발행 -> 문열림 경고 HUD OFF
                EventBus.Publish(new CellInspectionCompletedEvent
                {
                    CellId = cellId
                });
            }
            else PlayLocked();
        }
    }

    // ✅ 죄수를 찾아 상태를 변경하는 함수
    private void TriggerPrisonerInspection()
    {
        if (contentRegistry == null) return;

        if (contentRegistry.TryGet(cellId, out var content))
        {
            if (content.prisoner != null)
            {
                var fsm = content.prisoner.GetComponent<PrisonerFSM>();
                if (fsm != null)
                {
                    // FSM 상태 변경! (이때 죄수가 일어서서 걸어 나옴)
                    fsm.ChangeState(fsm.InspectionState);
                    if (verboseLog) Debug.Log($"[Door] {cellId} 죄수에게 점검 상태 명령 전달.");
                }
            }
        }
    }

    private bool TryEnter()
    {
        if (cellManager == null) return false;

        var cell = cellManager.GetCell(cellId);
        if (cell == null)
        {
            Debug.LogError($"[Door] CellManager에서 ID '{cellId}'를 찾을 수 없습니다. 오타 확인 필요.");
            return false;
        }

        if (cell.IsLockedForDay)
        {
            Debug.Log($"[Door] {cellId}는 금일 폐쇄(IsLockedForDay) 상태입니다.");
            return false;
        }

        // InspectionStateMachine에서 거부하는 경우
        bool canEnter = inspection.TryEnterCell(cellId);
        if (!canEnter) Debug.Log($"[Door] InspectionStateMachine.TryEnterCell('{cellId}')가 false를 반환했습니다.");

        return canEnter;
    }

    private bool TryExit() => inspection.RequestExitCell(cellId);

    private void PlayOpen()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(CloseHash);
        doorAnimator.SetTrigger(OpenHash);

    }

    private void PlayClose()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(OpenHash);
        doorAnimator.SetTrigger(CloseHash);

        if (useRedOutlineOnCloseOnlySlidingDoor && outliner != null)
            outliner.SetHighlight(true, Color.red);
    }

    private void PlayLocked()
    {
        if (doorAnimator != null)
            doorAnimator.SetTrigger(LockedHash);

        if (outliner != null) outliner.SetHighlight(true, Color.red);
    }

    private bool Validate()
    {
        if (doorAnimator == null) return false;
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
            if (cellManager == null) cellManager = FindObjectOfType<PrisonManager>();
            if (contentRegistry == null) contentRegistry = FindObjectOfType<CellContentRegistry>();
            if (inspection == null || cellManager == null || contentRegistry == null) return false;
        }
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInside = false;
        }
    }
}