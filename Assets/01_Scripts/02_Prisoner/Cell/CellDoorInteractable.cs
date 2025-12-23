using UnityEngine;

public sealed class CellDoorInteractable : MonoBehaviour, IInteractable
{
    private static class AnimParams
    {
        public const string OpenTrigger = "Open";
        public const string CloseTrigger = "Close";
        public const string LockedTrigger = "Locked";
    }

    [Header("Identity")]
    [Tooltip("비어있으면 일반 문(단순 개폐)으로 동작합니다.")]
    [SerializeField] private string cellId;

    [SerializeField] private Collider cellInsideTrigger; // ✅ 감방 내부를 덮는 트리거 콜라이더

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry; // ✅ 죄수를 찾기 위해 필요
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    [Header("State")]
    [SerializeField] private bool _isPlayerInside; // ✅ 태그 검사로 상태 업데이트됨

    private bool _isSimpleDoorOpen = false;

    public void Interact(Player player)
    {
        if (!Validate()) return;

        if (doorAnimator.IsInTransition(0) ||
            doorAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)

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
        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell)
        {
            // [문 열기] 점검 시작
            if (TryEnter())
            {
                PlayOpen();

                // ✅ 핵심: 죄수 상태를 Idle -> Inspection으로 변경
                TriggerPrisonerInspection();
            }
            else
            {
                PlayLocked();
            }
        }
        else
        {
            // ✅ 개선 2: 태그 검사 기반 내부 체크
            if (_isPlayerInside)
            {
                Debug.LogWarning($"[Door] {cellId} 내부에 플레이어가 있어 문을 닫을 수 없습니다.");
                return;
            }

            if (TryExit())
            {
                PlayClose();
                inspection.CompleteInspection(cellId, inspection.IsSuppressionCleared);
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
        var cell = cellManager.GetCell(cellId);
        if (cell == null || cell.IsLockedForDay) return false;
        return inspection.TryEnterCell(cellId);
    }

    private bool TryExit() => inspection.RequestExitCell(cellId);

    private void PlayOpen()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(AnimParams.CloseTrigger);
        doorAnimator.SetTrigger(AnimParams.OpenTrigger);
    }

    private void PlayClose()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(AnimParams.OpenTrigger);
        doorAnimator.SetTrigger(AnimParams.CloseTrigger);
    }

    private void PlayLocked()
    {
        if (doorAnimator == null) return;
        if (HasParam(AnimParams.LockedTrigger)) doorAnimator.SetTrigger(AnimParams.LockedTrigger);
    }

    private bool Validate()
    {
        if (doorAnimator == null) return false;
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
            if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
            if (contentRegistry == null) contentRegistry = FindObjectOfType<CellContentRegistry>();
            if (inspection == null || cellManager == null || contentRegistry == null) return false;
        }
        return true;
    }

    private bool HasParam(string name)
    {
        foreach (var p in doorAnimator.parameters)
            if (p.name == name) return true;
        return false;
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