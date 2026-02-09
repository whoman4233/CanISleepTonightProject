using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public sealed class CellDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip("비어있으면 일반 문(단순 개폐)으로 동작합니다.")]
    [SerializeField] private string cellId;

    [SerializeField] private Collider cellInsideTrigger;

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry;
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [Header("Settings")]
    [SerializeField] private float interactCooldown = 0.8f;
    private float _lastInteractTime = -999f;
    [SerializeField] private InteractableOutliner outliner;

    [Header("Door SFX")]
    [SerializeField] private AudioClip slidingOpenClip;
    [SerializeField] private AudioClip slidingCloseClip;
    [SerializeField] private AudioClip hingedOpenClip;
    [SerializeField] private AudioClip hingedCloseClip;

    [SerializeField] private bool _isPlayerInside;
    private bool _isSimpleDoorOpen = false;

    // ★ 실제 문이 시각적으로 열려있는지 체크하는 변수
    private bool _isVisuallyOpen = false;

    private Coroutine _autoCloseCoroutine;

    private static readonly int OpenHash = Animator.StringToHash("Open");
    private static readonly int CloseHash = Animator.StringToHash("Close");
    private static readonly int LockedHash = Animator.StringToHash("Locked");

    private void Awake()
    {
        if (outliner == null) outliner = GetComponentInChildren<InteractableOutliner>(true);
        if (outliner == null) outliner = GetComponent<InteractableOutliner>();
    }

    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            PrisonerEventBus.OnForceOpenDoor += HandleForceOpen;
        }
    }

    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            PrisonerEventBus.OnForceOpenDoor -= HandleForceOpen;
        }
    }

    // ========================================================================
    // ★ [핵심 수정 1] 강제 개방 시 시스템(StateMachine)에 보고하지 않음
    // ========================================================================
    private void HandleForceOpen(string targetCellId)
    {
        if (this.cellId != targetCellId) return;

        if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방 (시스템 보고 안함)");

        // 애니메이션과 시각적 상태만 변경하고, EventBus나 InspectionState는 건드리지 않음
        // 이렇게 하면 시스템은 이 문이 '닫혀있다'고 판단하므로 다른 문을 여는데 방해되지 않음
        PlayOpen();
    }

    public void Interact(Player player)
    {
        if (!Validate()) return;

        if (Time.time < _lastInteractTime + interactCooldown) return;
        _lastInteractTime = Time.time;

        if (string.IsNullOrWhiteSpace(cellId))
        {
            HandleSimpleDoor();
            return;
        }

        HandlePrisonDoor();
    }

    // ... (HandleSimpleDoor, CoAutoCloseSimpleDoor 등은 기존과 동일하므로 생략 가능하나 전체 코드 유지를 위해 포함) ...
    private void HandleSimpleDoor()
    {
        var missionManager = DailyMissionManager.Instance;
        if (missionManager != null && missionManager.CurrentMission != null && !missionManager.IsBriefingDialogueViewed)
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("교도소장에게 오늘 미션에 대해 묻기", 2.0f, true));
            PlayLocked();
            return;
        }

        if (!_isSimpleDoorOpen)
        {
            PlayOpen();
            _isSimpleDoorOpen = true;
            if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
            _autoCloseCoroutine = StartCoroutine(CoAutoCloseSimpleDoor());
        }
        else
        {
            PlayClose();
            _isSimpleDoorOpen = false;
            if (_autoCloseCoroutine != null) StopCoroutine(_autoCloseCoroutine);
        }
    }

    private IEnumerator CoAutoCloseSimpleDoor()
    {
        yield return new WaitForSeconds(5.0f);
        if (_isSimpleDoorOpen) HandleSimpleDoor();
    }

    // ========================================================================
    // ★ [핵심 수정 2] 상호작용 로직 개선
    // ========================================================================
    private void HandlePrisonDoor()
    {
        if (inspection == null) return;

        // "내가 정식으로 점검 중인 방인가?"
        bool isOfficialInspection = inspection.CurrentInspectingCellId == cellId;

        // "눈에 보이기에 열려 있는가?" (강제 개방 포함)
        bool isOpen = isOfficialInspection || _isVisuallyOpen;

        if (isOpen)
        {
            // 열려 있으면 -> 닫기 시도
            TryCloseDoor(isOfficialInspection);
        }
        else
        {
            // 닫혀 있으면 -> 열기(점검 진입) 시도
            TryOpenDoor();
        }
    }

    private void TryOpenDoor()
    {
        // 1. 잠긴 방 체크
        if (cellManager != null)
        {
            var cell = cellManager.GetCell(cellId);
            if (cell != null && cell.IsLockedForDay)
            {
                EventBus.Publish(new ShowTimedTextPopupEvent("잠겨 있는 방입니다.", 2.0f, true));
                PlayLocked();
                return;
            }
        }

        // 2. 시스템 점검 진입 시도
        // 강제 개방된 문들은 시스템상 '닫힘' 상태이므로, 여기서 TryEnterCell을 호출해도 
        // "다른 문이 열려있다"며 막히는 일이 발생하지 않음.
        if (TryEnter())
        {
            if (verboseLog) Debug.Log($"[Door] {cellId}: 문 열기 성공 & 점검 시작");
            EventBus.Publish(new CellInspectionInProgressEvent { CellId = cellId });
            PlayOpen();
            TriggerPrisonerInspection();
        }
        else
        {
            // 다른 방을 '정식 점검' 중일 때만 여기가 실행됨
            Debug.LogWarning($"[Door] {cellId}: 진입 불가. 다른 방 점검 중.");
            EventBus.Publish(new ShowTimedTextPopupEvent("다른 감방 문을 닫아야 열 수 있습니다.", 2.0f, true));
            PlayLocked();
        }
    }

    private void TryCloseDoor(bool isOfficialInspection)
    {
        // 1. 플레이어 내부 체크
        if (cellInsideTrigger != null && _isPlayerInside)
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("내부에선 문을 닫을 수 없습니다.", 2.0f, true));
            return;
        }

        // 2. 전투/도주 체크
        if (IsCombatInProgress() || IsEscapeInProgress())
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("비상 상황! 문을 닫을 수 없습니다!", 2.0f, true));
            PlayLocked();
            return;
        }

        // 3. 닫기 실행
        PlayClose(); // 물리적으로 닫기

        if (isOfficialInspection)
        {
            // 정식 점검 중이었다면 시스템 점검 종료 처리
            if (TryExit())
            {
                inspection.CompleteInspection(cellId, inspection.IsSuppressionCleared);
                EventBus.Publish(new CellInspectionCompletedEvent { CellId = cellId });
            }
        }
        else
        {
            // 강제 개방 상태였다면 그냥 문만 닫으면 됨 (시스템은 이미 닫힌 줄 아니까)
            if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방된 문 닫음.");
        }
    }

    // ... (IsCombatInProgress, IsEscapeInProgress, TriggerPrisonerInspection 유지) ...
    private bool IsCombatInProgress()
    {
        if (contentRegistry == null) return false;
        if (contentRegistry.TryGet(cellId, out var content) && content.prisoner != null)
        {
            var fsm = content.prisoner.GetComponent<PrisonerFSM>();
            if (fsm != null && (fsm._currentState is PrisonerCombatState || fsm._currentState is PrisonerCowerState))
                return true;
        }
        return false;
    }

    private bool IsEscapeInProgress()
    {
        if (contentRegistry == null) return false;
        if (contentRegistry.TryGet(cellId, out var content) && content.prisoner != null)
        {
            var fsm = content.prisoner.GetComponent<PrisonerFSM>();
            if (fsm != null && fsm._currentState is PrisonerEscapeState)
                return true;
        }
        return false;
    }

    private void TriggerPrisonerInspection()
    {
        if (contentRegistry == null) return;
        if (contentRegistry.TryGet(cellId, out var content) && content.prisoner != null)
        {
            var fsm = content.prisoner.GetComponent<PrisonerFSM>();
            if (fsm != null) fsm.OnStartInspection();
        }
    }

    private bool TryEnter()
    {
        if (cellManager == null) return false;
        return inspection.TryEnterCell(cellId);
    }

    private bool TryExit() => inspection.RequestExitCell(cellId);

    private void PlayOpen()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(CloseHash);
        doorAnimator.SetTrigger(OpenHash);
        _isVisuallyOpen = true; // ★ 시각적 상태 True
        PlayOpenSound();
    }

    private void PlayClose()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(OpenHash);
        doorAnimator.SetTrigger(CloseHash);
        _isVisuallyOpen = false; // ★ 시각적 상태 False
        PlayCloseSound();
    }

    private void PlayLocked()
    {
        if (doorAnimator != null) doorAnimator.SetTrigger(LockedHash);
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
        if (other.CompareTag("Player")) _isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) _isPlayerInside = false;
    }

    public OpenClosePromptState GetPromptStateEnum()
    {
        if (string.IsNullOrWhiteSpace(cellId))
            return _isSimpleDoorOpen ? OpenClosePromptState.Open : OpenClosePromptState.Close;

        if (inspection == null) return OpenClosePromptState.Close;

        // 프롬프트 표시 기준: 정식 점검 중이거나 OR 시각적으로 열려있으면 '닫기' 표시
        bool isOfficialInspection = inspection.CurrentInspectingCellId == cellId;
        bool isOpen = isOfficialInspection || _isVisuallyOpen;

        if (!isOpen) return OpenClosePromptState.Close; // 닫혀있으면 Open(열기)

        // 열려있는 상태에서의 예외 처리
        if (_isPlayerInside) return OpenClosePromptState.CannotClose;
        if (IsCombatInProgress()) return OpenClosePromptState.CannotClose;

        return OpenClosePromptState.Open; // 열려있으면 Close(닫기)
    }

    private void PlayOpenSound()
    {
        AudioClip clip = string.IsNullOrWhiteSpace(cellId) ? hingedOpenClip : slidingOpenClip;
        if (clip != null) AudioManager.Instance.PlaySFX(clip);
    }

    private void PlayCloseSound()
    {
        AudioClip clip = string.IsNullOrWhiteSpace(cellId) ? hingedCloseClip : slidingCloseClip;
        if (clip != null) AudioManager.Instance.PlaySFX(clip);
    }
}