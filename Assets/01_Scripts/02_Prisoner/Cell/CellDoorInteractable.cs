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
    [SerializeField] private CellContentRegistry contentRegistry; // ✅ 죄수를 찾기 위해 필요
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private bool _isSimpleDoorOpen = false;

    public void Interact(Player player)
    {
        if (!Validate()) return;

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
            // [문 닫기] 점검 종료
            if (TryExit())
            {
                PlayClose();

                // 리포트 기록 및 시스템 리셋 (다른 방 문 열 수 있게 함)
                bool didSuppress = inspection.IsSuppressionCleared;

                // InspectionStateMachine에 우리가 만든 Complete 로직이 있다면 그것을 사용
                // 없으면 아래처럼 직접 처리
                cellManager.MarkResolvedAndLockForDay(cellId, didSuppress);
                inspection.EndInspection();

                if (verboseLog) Debug.Log($"[Door] {cellId} closed. System Reset.");
            }
            else
            {
                PlayLocked();
            }
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
}