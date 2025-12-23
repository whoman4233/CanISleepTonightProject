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

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    // cellId가 없는 일반 문일 때만 사용하는 내부 상태값
    private bool _isSimpleDoorOpen = false;

    public void Interact(Player player)
    {
        if (!Validate()) return;

        // 1. 일반 문 로직 (cellId가 없는 경우)
        if (string.IsNullOrWhiteSpace(cellId))
        {
            if (!_isSimpleDoorOpen)
            {
                PlayOpen();
                _isSimpleDoorOpen = true;
            }
            else
            {
                PlayClose();
                _isSimpleDoorOpen = false;
            }
            return;
        }

        // 2. 감방 전용 로직 (cellId가 있는 경우)
        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell)
        {
            // [문 열기] 점검 시작 시도
            if (TryEnter())
                PlayOpen();
            else
                PlayLocked();
        }
        else
        {
            // [문 닫기] 점검 종료 시도
            if (TryExit())
            {
                PlayClose();
                bool didSuppress = inspection.IsSuppressionCleared;
                cellManager.MarkResolvedAndLockForDay(cellId, didSuppress);

                if (verboseLog) Debug.Log($"[Door] {cellId} closed and locked for the day.");
            }
            else
            {
                PlayLocked();
            }
        }
    }

    private bool TryEnter()
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null) return false;

        if (cell.IsLockedForDay) return false;

        return inspection.TryEnterCell(cellId);
    }

    private bool TryExit()
    {
        return inspection.RequestExitCell(cellId);
    }

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
        if (HasParam(AnimParams.LockedTrigger))
            doorAnimator.SetTrigger(AnimParams.LockedTrigger);
    }

    private bool Validate()
    {
        // Animator는 어떤 상황에서도 필수입니다.
        if (doorAnimator == null)
        {
            Debug.LogError("[Door] Animator is missing.", this);
            return false;
        }

        // cellId가 있을 때만 Manager와 StateMachine이 필수입니다.
        if (!string.IsNullOrWhiteSpace(cellId))
        {
            if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
            if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();

            if (inspection == null || cellManager == null)
            {
                Debug.LogWarning("[Door] Missing refs for cell logic.", this);
                return false;
            }
        }

        return true;
    }

    private bool HasParam(string name)
    {
        if (doorAnimator == null) return false;
        foreach (var p in doorAnimator.parameters)
            if (p.name == name) return true;
        return false;
    }
}