using System;
using UnityEngine;

public class InspectionStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;

    public string CurrentInspectingCellId { get; private set; }

    // 외부(UI/시스템/정산) 연동 이벤트
    public event Action<string> OnEnteredCell;                         // cellId
    public event Action<string> OnExitBlocked;                         // cellId
    public event Action<string, bool, bool> OnResolved;                // cellId, isSuspicious, didSuppress
    public event Action<string> OnSuppressStarted;                     // cellId
    public event Action<string> OnSuppressSuccess;                     // cellId

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        cellManager?.BuildCellsIfNeeded();
    }

    public bool TryEnterCell(string cellId)
    {
        if (cellManager == null) return false;

        // 한 번에 한 방만 점검
        if (!string.IsNullOrEmpty(CurrentInspectingCellId))
            return false;

        var cell = cellManager.GetCell(cellId);
        if (cell == null) return false;

        // 오늘 활성(소음) 방만 점검 가능
        if (!cell.IsActiveToday || !cell.IsNoisy)
            return false;

        cell.IsInspectingNow = true;
        cell.State = CellState.Inspecting;
        CurrentInspectingCellId = cellId;

        OnEnteredCell?.Invoke(cellId);
        return true;
    }

    // UI상 "경고" 버튼. 내부는 NonSuppress.
    public bool SelectWarning(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;

        // 진압 중에는 경고 선택 불가
        if (cell.State == CellState.Suppressing || cell.IsSuppressing)
            return false;

        cell.NonSuppressChosen = true;
        return true;
    }

    // 진압 선택: 즉시 퇴장/문 상호작용 잠금
    public bool SelectSuppress(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;

        if (cell.State == CellState.Suppressing) return false;

        cell.IsSuppressing = true;
        cell.SuppressSuccess = false;
        cell.State = CellState.Suppressing;

        OnSuppressStarted?.Invoke(cellId);
        PrisonerEventBus.RaiseSuppressSessionStarted(cellId);

        return true;
    }

    // 죄수 파트가 "진압 성공" 시 호출
    public bool NotifySuppressSuccess(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;

        if (cell.State != CellState.Suppressing) return false;

        cell.SuppressSuccess = true;
        OnSuppressSuccess?.Invoke(cellId);
        return true;
    }

    // 퇴장 요청: 규칙에 따라 허용/차단
    public bool RequestExitCell(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;

        // 진압 중이면 퇴장 잠김 (성공 전까지)
        if (cell.State == CellState.Suppressing)
        {
            if (!cell.SuppressSuccess)
            {
                OnExitBlocked?.Invoke(cellId);
                return false;
            }
            // 성공이면 퇴장 허용 (이 순간 Resolved 처리)
            Resolve(cell, didSuppress: true);
            return true;
        }

        // Inspecting 상태에서 경고/무시로 나가는 경우: 즉시 Resolved
        Resolve(cell, didSuppress: false);
        return true;
    }

    // 시간 초과: Exit 호출 X, 완료처리 X, 락만 해제하고 ActiveNoisy 유지
    public void ForceReleaseOnTimeExpired()
    {
        if (cellManager == null) return;
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;

        var cellId = CurrentInspectingCellId;
        var cell = cellManager.GetCell(cellId);
        if (cell == null)
        {
            CurrentInspectingCellId = null;
            return;
        }

        cellManager.ForceReleaseInspectingOnly(cellId);
        CurrentInspectingCellId = null;
    }

    private void Resolve(CellRuntime cell, bool didSuppress)
    {
        // Resolved 기록 이벤트 먼저 발행(외부 정산용)
        OnResolved?.Invoke(cell.CellId, cell.IsSuspicious, didSuppress);

        // 상태 정리 및 비활성
        cellManager.ResolveAndDeactivateCell(cell.CellId);

        // 점검 락 해제
        CurrentInspectingCellId = null;
    }

    private CellRuntime GetCurrentCellOrNull(string cellId)
    {
        if (cellManager == null) return null;
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return null;
        if (!string.Equals(CurrentInspectingCellId, cellId, StringComparison.Ordinal)) return null;
        return cellManager.GetCell(cellId);
    }

    private void OnEnable()
    {
        PrisonerEventBus.OnAllPrisonersDown += HandleAllDown;
    }

    private void OnDisable()
    {
        PrisonerEventBus.OnAllPrisonersDown -= HandleAllDown;
    }

    private void HandleAllDown(string cellId)
    {
        if (CurrentInspectingCellId != cellId) return;
        NotifySuppressSuccess(cellId);
    }

}
