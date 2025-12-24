using System;
using UnityEngine;

public class InspectionStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry; // 죄수 FSM을 찾기 위해 필요

    public string CurrentInspectingCellId { get; private set; }

    public event Action<string> OnEnteredCell;
    public event Action<string> OnExitBlocked;
    public event Action<string, bool, bool> OnResolved;
    public event Action<string> OnSuppressStarted;
    public event Action<string> OnSuppressSuccess;

    private bool _isSuppressionCleared;
    public bool IsSuppressionCleared => _isSuppressionCleared;

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (contentRegistry == null) contentRegistry = FindObjectOfType<CellContentRegistry>();
        cellManager?.BuildCellsIfNeeded();
    }

    private void OnEnable() => PrisonerEventBus.OnPrisonerDown += HandlePrisonerDown;
    private void OnDisable() => PrisonerEventBus.OnPrisonerDown -= HandlePrisonerDown;

    public bool TryEnterCell(string cellId)
    {
        if (cellManager == null || !string.IsNullOrEmpty(CurrentInspectingCellId)) return false;

        var cell = cellManager.GetCell(cellId);
        if (cell == null || !cell.IsActiveToday || cell.IsLockedForDay) return false;

        // 시스템 상태 변경
        cell.IsInspectingNow = true;
        cell.State = CellState.Inspecting;
        CurrentInspectingCellId = cellId;
        _isSuppressionCleared = false;

        // ✅ [살려야 할 로직 1] 죄수 FSM을 Inspection(일어서기) 상태로 전환
        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.InspectionState));

        OnEnteredCell?.Invoke(cellId);
        return true;
    }

    public void ForceReleaseOnTimeExpired()
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;

        var cellId = CurrentInspectingCellId;
        var cell = cellManager.GetCell(cellId);

        // ✅ 추가: 시간 다 되면 죄수를 다시 Idle(앉기) 상태로 돌려보냄
        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.IdleState));

        if (cell != null) cellManager.ForceReleaseInspectingOnly(cellId);

        CurrentInspectingCellId = null;
        Debug.Log($"[ISSM] Time Expired. Force Released cell {cellId}");
    }

    public void CompleteInspection(string cellId, bool didSuppress)
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null) return;

        // 리포트 빌더에 신호 전달
        OnResolved?.Invoke(cell.CellId, cell.IsSuspicious, didSuppress);
        // 데이터상 오늘 마감 처리
        cellManager.MarkResolvedAndLockForDay(cellId, didSuppress);
        // 시스템 리셋
        EndInspection();

        Debug.Log($"{cellId} complete");
    }

    public void EndInspection()
    {
        CurrentInspectingCellId = null;
        _isSuppressionCleared = false;
    }

    public bool RequestExitCell(string cellId)
    { 
        return true; // 무조건 문 닫기 허용
    }

    public bool SelectSuppress(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null || cell.State == CellState.Suppressing) return false;

        cell.IsSuppressing = true;
        cell.State = CellState.Suppressing;

        // ✅ 추가: 진압 버튼 클릭 시 죄수를 Combat(공격) 상태로 전환
        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.CombatState));

        OnSuppressStarted?.Invoke(cellId);
        PrisonerEventBus.RaiseSuppressSessionStarted(cellId);
        return true;
    }

    // --- 헬퍼 메서드 ---
    private void SetPrisonerState(string cellId, Action<PrisonerFSM> action)
    {
        if (contentRegistry != null && contentRegistry.TryGet(cellId, out var content))
        {
            if (content.prisoner != null)
            {
                var fsm = content.prisoner.GetComponent<PrisonerFSM>();
                if (fsm != null) action?.Invoke(fsm);
            }
        }
    }

    private void HandlePrisonerDown(string instanceId)
    {
        if (!string.IsNullOrEmpty(CurrentInspectingCellId) && instanceId.StartsWith(CurrentInspectingCellId))
        {
            _isSuppressionCleared = true;
            NotifySuppressSuccess(CurrentInspectingCellId);
        }
    }

    public bool NotifySuppressSuccess(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null || cell.State != CellState.Suppressing) return false;
        cell.SuppressSuccess = true;
        OnSuppressSuccess?.Invoke(cellId);
        return true;
    }

    private CellRuntime GetCurrentCellOrNull(string cellId)
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId) || !CurrentInspectingCellId.Equals(cellId)) return null;
        return cellManager.GetCell(cellId);
    }
}