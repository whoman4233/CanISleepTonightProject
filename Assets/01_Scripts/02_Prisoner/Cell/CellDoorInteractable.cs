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

    // 강제 개방 시에도 시스템에 물리적 개방 상태 보고
    private void HandleForceOpen(string targetCellId)
    {
        if (this.cellId != targetCellId) return;
        if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방 보고");

        if (inspection != null) inspection.ReportPhysicalOpen(cellId);
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
        // ★ [추가] 감방 문이 하나라도 열려 있으면 계단 문 상호작용 차단
        bool isAnyDoorOpen = !string.IsNullOrEmpty(inspection.CurrentInspectingCellId) ||
                             !string.IsNullOrEmpty(inspection.PhysicallyOpenedCellId);

        if (isAnyDoorOpen)
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_53", 2.0f, true));
            PlayLocked();
            return;
        }

        var missionManager = DailyMissionManager.Instance;
        if (missionManager != null && missionManager.CurrentMission != null && !missionManager.IsBriefingDialogueViewed)
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_51", 2.0f, true));
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
        yield return new WaitForSeconds(3.0f);
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
    //  7번 미션 예외 처리 및 중복 개방 차단
    // ========================================================================
    private void TryOpenDoor()
    {
        // 1. 미션 07(폭동) 상황인지 체크
        bool isRiotMission = (DailyMissionManager.Instance?.CurrentMission.missionId == DialogueKeys.Missions.Mission07);

        // 2. 시스템 점검 시도
        if (TryEnter())
        {
            if (verboseLog) Debug.Log($"[Door] {cellId}: 문 열기 성공 & 점검 시작");
            EventBus.Publish(new CellInspectionInProgressEvent { CellId = cellId });
            PlayOpen();
            TriggerPrisonerInspection();
        }
        else
        {
            // 3. 점검 진입 실패 시

            // A. 미션 07 상황이라면 시스템 점검은 안 되더라도 물리적 개방 허용 (중복 열기)
            if (isRiotMission)
            {
                if (!IsLockedForDay())
                {
                    if (verboseLog) Debug.Log($"[Door] {cellId}: 미션 07 중복 개방 허용");
                    inspection.ReportPhysicalOpen(cellId);
                    PlayOpen();
                    TriggerPrisonerInspection();
                }
                else
                {
                    ShowLockedPopup();
                }
                return;
            }

            // B. 일반적인 상황에서 중복 개방 시도인 경우 (경고 출력)
            string openedId = !string.IsNullOrEmpty(inspection.CurrentInspectingCellId)
                              ? inspection.CurrentInspectingCellId
                              : inspection.PhysicallyOpenedCellId;

            if (!string.IsNullOrEmpty(openedId))
            {
                if (verboseLog) Debug.Log($"[Door] {cellId}: 중복 개방 차단. 현재 열린 문: {openedId}");
                EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_54", 2.0f, true));
                PlayLocked();
            }
            // C. 잠긴 방인 경우
            else if (IsLockedForDay())
            {
                ShowLockedPopup();
            }
            // D. 기타 상황 물리적 개방 (백업 로직)
            else
            {
                PlayOpen();
                TriggerPrisonerInspection();
            }
        }
    }

    private void ShowLockedPopup()
    {
        EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_56", 2.0f, true));
        PlayLocked();
    }

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
            EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_57", 2.0f, true));
            return;
        }

        if (IsCombatInProgress() || IsEscapeInProgress())
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("Utxt_KR_55", 2.0f, true));
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
            // 강제 개방 혹은 중복 개방된 문을 닫을 때도 시스템 보고 해제
            if (inspection != null) inspection.ReportPhysicalClose(cellId);
            if (verboseLog) Debug.Log($"[Door] {cellId}: 비공식 문 닫음 보고");
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

        // ★ 물리적으로 닫힐 때 시스템에 항상 보고
        if (inspection != null) inspection.ReportPhysicalClose(cellId);

        PlayCloseSound();
    }

    private void PlayLocked()
    {
        if (doorAnimator != null) doorAnimator.SetTrigger(LockedHash);
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