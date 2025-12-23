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

    // 일반 문 전용 상태 (cellId가 없을 때만 사용)
    private bool _isSimpleDoorOpen = false;

    public void Interact(Player player)
    {
        if (!Validate()) return;

        // 1. 일반 문 (단순 가구/사무실 문 등)
        if (string.IsNullOrWhiteSpace(cellId))
        {
            HandleGenericDoor();
            return;
        }

        // 2. 감방 문 (로직 필요)
        HandlePrisonCellDoor();
    }

    private void HandleGenericDoor()
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
    }

    private void HandlePrisonCellDoor()
    {
        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell)
        {
            if (TryEnter()) PlayOpen();
            else PlayLocked();
        }
        else
        {
            if (TryExit())
            {
                PlayClose();

                // ✅ 기존의 cellManager 직접 호출과 inspection.EndInspection()을 
                // ✅ 하나로 묶은 CompleteInspection으로 대체합니다.
                bool didSuppress = inspection.IsSuppressionCleared;
                inspection.CompleteInspection(cellId, didSuppress);

                if (verboseLog) Debug.Log($"[Door] {cellId} 점검 및 리포트 기록 완료.");
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

        // 오늘 이미 해결된 방이면 입장 불가
        if (cell.IsLockedForDay) return false;

        // 상태 머신에게 입장 가능 여부 확인
        return inspection.TryEnterCell(cellId);
    }

    private bool TryExit()
    {
        // 상태 머신에게 퇴장 가능 여부 확인 (진압 여부 체크 포함)
        return inspection.RequestExitCell(cellId);
    }

    // --- 애니메이션 제어 헬퍼 ---
    public void PlayOpen()
    {
        if (doorAnimator == null) return;
        doorAnimator.ResetTrigger(AnimParams.CloseTrigger);
        doorAnimator.SetTrigger(AnimParams.OpenTrigger);
    }

    public void PlayClose()
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
        if (doorAnimator == null) return false;

        if (!string.IsNullOrWhiteSpace(cellId))
        {
            if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
            if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();

            if (inspection == null || cellManager == null) return false;
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