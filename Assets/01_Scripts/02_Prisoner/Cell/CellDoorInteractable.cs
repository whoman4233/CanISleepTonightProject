using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public sealed class CellDoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip("비어있으면 일반 문(단순 개폐)으로 동작합니다.")]
    [SerializeField] private string cellId;

    [SerializeField] private Collider cellInsideTrigger; // 감방 내부를 덮는 트리거 콜라이더

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
    [SerializeField] private bool useRedOutlineOnCloseOnlySlidingDoor = false;
    [SerializeField] private InteractableOutliner outliner;

    [Header("Door SFX")]
    [SerializeField] private AudioClip slidingOpenClip;
    [SerializeField] private AudioClip slidingCloseClip;
    [SerializeField] private AudioClip hingedOpenClip;
    [SerializeField] private AudioClip hingedCloseClip;

    [SerializeField] private bool _isPlayerInside;
    private bool _isSimpleDoorOpen = false;
    private Coroutine _autoCloseCoroutine;

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

    private void HandleForceOpen(string targetCellId)
    {
        if (this.cellId != targetCellId) return;

        if (verboseLog) Debug.Log($"[Door] {cellId}: 강제 개방 요청 수신 (Ambush)");

        PlayOpen();
    }

    public void Interact(Player player)
    {
        if (!Validate()) return;

        if (Time.time < _lastInteractTime + interactCooldown) return;
        _lastInteractTime = Time.time;

        if (string.IsNullOrWhiteSpace(cellId))
        {
            if (inspection != null && !string.IsNullOrEmpty(inspection.CurrentInspectingCellId))
            {
                Debug.LogError($"[Door Blocked] 계단 문을 열 수 없습니다! 현재 열려있는 감방: {inspection.CurrentInspectingCellId}");
                EventBus.Publish(new ShowTimedTextPopupEvent("감방 문이 열려있습니다! 문을 닫고 이동하세요.", 2.0f, true));
                PlayLocked();
                return;
            }

            HandleSimpleDoor();
            return;
        }

        HandlePrisonDoor();
    }

    private void HandleSimpleDoor()
    {
        var missionManager = DailyMissionManager.Instance;

        if (missionManager != null &&
            missionManager.CurrentMission != null &&
            !missionManager.IsBriefingDialogueViewed)
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

        if (_isSimpleDoorOpen)
        {
            if (verboseLog) Debug.Log($"[Door] 일반 문 5초 경과하여 자동 닫힘");
            HandleSimpleDoor();
        }
    }

    private void HandlePrisonDoor()
    {
        if (inspection == null)
        {
            Debug.LogError($"[Door] {name}: InspectionStateMachine 연결 안됨!");
            return;
        }

        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        // [상황 1] 문이 닫혀있어서 열어야 함 (점검 시작)
        if (!isInspectingThisCell)
        {
            if (TryEnter())
            {
                if (verboseLog) Debug.Log($"[Door] {cellId}: 문 열기 성공 & 점검 시작");
                EventBus.Publish(new CellInspectionInProgressEvent { CellId = cellId });
                PlayOpen();
                TriggerPrisonerInspection();
            }
            else
            {
                Debug.LogWarning($"[Door] {cellId}: 진입 불가. 잠김 애니메이션.");
                EventBus.Publish(new ShowTimedTextPopupEvent("열려 있는 다른 감방 문을 닫아야 열 수 있습니다.", 2.0f, true));
                PlayLocked();
            }
        }
        // [상황 2] 문이 열려있어서 닫아야 함 (점검 종료 시도)
        else
        {
            if (cellInsideTrigger != null && _isPlayerInside)
            {
                EventBus.Publish(new ShowTimedTextPopupEvent("내부에선 문을 닫을 수 없습니다.", 2.0f, true));
                Debug.LogWarning($"[Door] {cellId} 내부에 플레이어가 있어 문을 닫을 수 없습니다.");
                return;
            }

            // ====================================================
            // ★ [추가] 전투 중 문 닫기 방지 로직
            // ====================================================
            // 플레이어가 불리하다고 문 닫고 도망가는 꼼수 방지
            if (IsCombatInProgress())
            {
                EventBus.Publish(new ShowTimedTextPopupEvent("전투 중에는 문을 닫을 수 없습니다!", 2.0f, true));
                Debug.LogWarning($"[Door] {cellId}: 전투 중이라 문을 닫을 수 없음.");

                // 문 흔들리는 연출(Locked)
                PlayLocked();
                return;
            }

            if (TryExit())
            {
                PlayClose();
                inspection.CompleteInspection(cellId, inspection.IsSuppressionCleared);
                EventBus.Publish(new CellInspectionCompletedEvent { CellId = cellId });
            }
            else
            {
                PlayLocked();
            }
        }
    }

    // ★ [추가] 해당 방의 죄수들이 전투 중인지 체크
    private bool IsCombatInProgress()
    {
        if (contentRegistry == null) return false;

        // 이 감방의 죄수 데이터를 가져옴
        if (contentRegistry.TryGet(cellId, out var content) && content.prisoner != null)
        {
            var fsm = content.prisoner.GetComponent<PrisonerFSM>();
            if (fsm != null)
            {
                // 죄수가 전투 상태(CombatState)이거나 이미 공격 모드라면 true
                // (PrisonerCombatState 타입 체크 방식이 가장 확실함)
                if (fsm._currentState is PrisonerCombatState || fsm._currentState is PrisonerCowerState)
                {
                    return true;
                }
            }
        }
        return false;
    }

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
                    fsm.OnStartInspection();
                    if (verboseLog) Debug.Log($"[Door] {cellId} 죄수에게 점검 신호 전달.");
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
            Debug.LogError($"[Door] CellManager에서 ID '{cellId}'를 찾을 수 없습니다.");
            return false;
        }

        bool canEnter = inspection.TryEnterCell(cellId);
        if (!canEnter) Debug.Log($"[Door] InspectionStateMachine 거부됨.");

        return canEnter;
    }

    private bool TryExit() => inspection.RequestExitCell(cellId);

    private void PlayOpen()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(CloseHash);
        doorAnimator.SetTrigger(OpenHash);
        PlayOpenSound();
    }

    private void PlayClose()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(OpenHash);
        doorAnimator.SetTrigger(CloseHash);

        PlayCloseSound();
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
            _isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _isPlayerInside = false;
    }

    public OpenClosePromptState GetPromptStateEnum()
    {
        if (string.IsNullOrWhiteSpace(cellId))
        {
            return _isSimpleDoorOpen ? OpenClosePromptState.Open : OpenClosePromptState.Close;
        }

        if (inspection == null) return OpenClosePromptState.Close;

        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell) return OpenClosePromptState.Close;

        if (_isPlayerInside) return OpenClosePromptState.CannotClose;

        // ★ [추가] 전투 중이면 닫을 수 없음 상태 반환 (UI 프롬프트 갱신용)
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