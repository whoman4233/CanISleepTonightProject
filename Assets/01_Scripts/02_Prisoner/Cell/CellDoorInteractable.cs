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

    // 실제 문이 시각적으로 열려있는지 체크하는 변수
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

    // 강제 개방 시 시스템(StateMachine)에 보고하지 않음 (이전 수정 유지)
    private void HandleForceOpen(string targetCellId)
    {
        if (this.cellId != targetCellId) return;
        if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방 (시스템 보고 안함)");
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

    private void HandlePrisonDoor()
    {
        if (inspection == null) return;

        bool isOfficialInspection = inspection.CurrentInspectingCellId == cellId;
        bool isOpen = isOfficialInspection || _isVisuallyOpen;

        if (isOpen)
        {
            TryCloseDoor(isOfficialInspection);
        }
        else
        {
            TryOpenDoor();
        }
    }

    // ========================================================================
    // ★ [핵심 수정] 문 열기 로직 개선 (재입장 허용 + 미션 잠금 준수)
    // ========================================================================
    private void TryOpenDoor()
    {
        // 1. 먼저 시스템(InspectionStateMachine)에 진입을 요청합니다.
        // 시스템이 재입장을 허용한다면(잠겨있더라도), TryEnterCell은 true를 반환할 것입니다.
        if (TryEnter())
        {
            if (verboseLog) Debug.Log($"[Door] {cellId}: 문 열기 성공 & 점검 시작");
            EventBus.Publish(new CellInspectionInProgressEvent { CellId = cellId });
            PlayOpen();
            TriggerPrisonerInspection();
        }
        else
        {
            // 2. 시스템이 진입을 거부했습니다. 이유를 판별합니다.

            // A. 진짜로 잠긴 방인가? (미션 06 등에서 강제 잠금된 경우)
            // 시스템도 거부하고 + IsLockedForDay도 true라면 -> 절대 열어주면 안 됨.
            if (IsLockedForDay())
            {
                if (verboseLog) Debug.Log($"[Door] {cellId}: 시스템 거부 및 잠금 상태 -> 열기 불가.");
                EventBus.Publish(new ShowTimedTextPopupEvent("잠겨 있는 방입니다.", 2.0f, true));
                PlayLocked();
            }
            // B. 잠기진 않았는데 시스템이 거부했는가? (다른 방이 열려있음 or 단순 시스템 Busy)
            // 이 경우는 사용자가 요청한 '무시하고 열기(강제 오픈)' 케이스에 해당합니다.
            else
            {
                if (verboseLog) Debug.Log($"[Door] {cellId}: 시스템 거부(Busy?) but 잠기진 않음 -> 물리적 개방.");
                PlayOpen();
                TriggerPrisonerInspection();
            }
        }
    }

    // 오늘 잠긴 방인지 확인하는 헬퍼
    private bool IsLockedForDay()
    {
        if (cellManager == null) return false;
        var cell = cellManager.GetCell(cellId);
        return cell != null && cell.IsLockedForDay;
    }

    private void TryCloseDoor(bool isOfficialInspection)
    {
        if (cellInsideTrigger != null && _isPlayerInside)
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("내부에선 문을 닫을 수 없습니다.", 2.0f, true));
            return;
        }

        if (IsCombatInProgress() || IsEscapeInProgress())
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("비상 상황! 문을 닫을 수 없습니다!", 2.0f, true));
            PlayLocked();
            return;
        }

        PlayClose(); // 물리적으로 닫기

        if (isOfficialInspection)
        {
            if (TryExit())
            {
                inspection.CompleteInspection(cellId, inspection.IsSuppressionCleared);
                EventBus.Publish(new CellInspectionCompletedEvent { CellId = cellId });
            }
        }
        else
        {
            if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방된 문 닫음.");
        }
    }

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
        _isVisuallyOpen = true;
        PlayOpenSound();
    }

    private void PlayClose()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(OpenHash);
        doorAnimator.SetTrigger(CloseHash);
        _isVisuallyOpen = false;
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

        bool isOfficialInspection = inspection.CurrentInspectingCellId == cellId;
        bool isOpen = isOfficialInspection || _isVisuallyOpen;

        if (!isOpen) return OpenClosePromptState.Close;

        if (_isPlayerInside) return OpenClosePromptState.CannotClose;
        if (IsCombatInProgress()) return OpenClosePromptState.CannotClose;

        return OpenClosePromptState.Open;
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