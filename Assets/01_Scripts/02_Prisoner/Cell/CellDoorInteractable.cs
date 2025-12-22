using UnityEngine;

public sealed class CellDoorInteractable : MonoBehaviour, IInteractable
{
    private static class AnimParams
    {
        public const string OpenTrigger = "Open";
        public const string CloseTrigger = "Close";
        public const string LockedTrigger = "Locked"; // 선택(없으면 지워도 됨)
    }

    [Header("Identity")]
    [SerializeField] private string cellId;

    [Header("Refs")]
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private Animator doorAnimator;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    public void Interact(Player player)
    {
        if (!Validate()) return;

        // 현재 이 방을 점검 중인지 확인
        bool isInspectingThisCell = inspection.CurrentInspectingCellId == cellId;

        if (!isInspectingThisCell)
        {
            // [문 열기] 점검 시작
            if (TryEnter())
            {
                PlayOpen();
                // 필요하다면 여기서 문이 열린 상태임을 기록
            }
            else
            {
                PlayLocked();
            }
        }
        else
        {
            // [문 닫기] 점검 종료 시도
            // TryExit 내부에서 검증(진압 완료 등)을 거친 후 문을 닫습니다.
            if (TryExit())
            {
                PlayClose();

                // ✅ 핵심: 문을 성공적으로 닫았다면, 방을 'LockedForDay' 상태로 변경
                // 진압 성공 여부는 inspection 상태 머신에서 가져옵니다.
                bool didSuppress = inspection.IsSuppressionCleared;
                cellManager.MarkResolvedAndLockForDay(cellId, didSuppress);

                if (verboseLog) Debug.Log($"[Door] {cellId} closed and locked for the day.");
            }
            else
            {
                // 아직 죄수가 안 죽었거나 점검이 안 끝났으면 철컥거리고 안 닫힘
                PlayLocked();
            }
        }
    }

    private bool TryEnter()
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null)
        {
            if (verboseLog) Debug.LogWarning($"[Door] CellRuntime not found cell={cellId}", this);
            return false;
        }

        // 오늘 재입장 금지
        if (cell.IsLockedForDay)
        {
            if (verboseLog) Debug.Log($"[Door] Enter blocked (LockedForDay) cell={cellId}", this);
            return false;
        }

        // 룰 체크 포함: 활성+소음+동시점검 1개 제한
        bool ok = inspection.TryEnterCell(cellId);
        if (!ok)
        {
            if (verboseLog) Debug.Log($"[Door] TryEnter failed cell={cellId}", this);
            return false;
        }

        if (verboseLog) Debug.Log($"[Door] Enter SUCCESS cell={cellId}", this);
        return true;
    }

    private bool TryExit()
    {
        bool ok = inspection.RequestExitCell(cellId); // 진압 중 성공 전이면 false
        if (!ok)
        {
            if (verboseLog) Debug.Log($"[Door] Exit blocked cell={cellId}", this);
            return false;
        }

        if (verboseLog) Debug.Log($"[Door] Exit SUCCESS cell={cellId}", this);
        return true;
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
        // 잠김 연출이 없으면 그냥 아무것도 안 해도 됨
        if (HasParam(AnimParams.LockedTrigger))
            doorAnimator.SetTrigger(AnimParams.LockedTrigger);
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(cellId))
        {
            Debug.LogWarning("[Door] cellId empty.", this);
            return false;
        }

        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();

        if (inspection == null || cellManager == null)
        {
            Debug.LogWarning("[Door] Missing refs (inspection/cellManager).", this);
            return false;
        }
        return true;
    }

    private bool HasParam(string name)
    {
        // Animator 파라미터 존재 체크는 비용이 있어서 MVP에선 생략해도 됩니다.
        // 필요하면 캐시 방식으로 바꿔드릴게요.
        foreach (var p in doorAnimator.parameters)
            if (p.name == name) return true;
        return false;
    }
}
