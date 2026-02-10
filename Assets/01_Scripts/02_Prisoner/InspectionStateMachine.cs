using System;
using UnityEngine;

public class InspectionStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry;

    // 공식적으로 점검 중인 방 (UI 및 점검 로직용)
    public string CurrentInspectingCellId { get; private set; }

    // ★ [추가] 물리적으로 열려 있는 방 ID (강제 개방 포함)
    // 이 값이 null이 아닐 때 다른 문 상호작용을 막는 용도로 사용합니다.
    public string PhysicallyOpenedCellId { get; private set; }

    public event Action<string> OnEnteredCell;
    public event Action<string, bool, bool> OnResolved;
    public event Action<string> OnSuppressStarted;
    public event Action<string> OnSuppressSuccess;

    private bool _isSuppressionCleared;
    public bool IsSuppressionCleared => _isSuppressionCleared;

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonManager>();
        if (contentRegistry == null) contentRegistry = FindObjectOfType<CellContentRegistry>();
    }

    private void OnEnable() => PrisonerEventBus.OnPrisonerDown += HandlePrisonerDown;
    private void OnDisable() => PrisonerEventBus.OnPrisonerDown -= HandlePrisonerDown;

    // =======================================================================
    // [추가] 물리적 상태 보고 (미션 7 강제 개방 등에서 호출)
    // =======================================================================
    public void ReportPhysicalOpen(string cellId)
    {
        PhysicallyOpenedCellId = cellId;
    }

    public void ReportPhysicalClose(string cellId)
    {
        if (PhysicallyOpenedCellId == cellId)
        {
            PhysicallyOpenedCellId = null;
        }
    }

    // =======================================================================
    // [1] 점검 진입 (Enter)
    // =======================================================================
    public bool TryEnterCell(string cellId)
    {
        if (cellManager == null) return false;

        // ★ [수정] 중복 진입 방지 로직 개선
        // 공식 점검 중인 방이 있거나, 물리적으로 이미 열린 방이 있다면 거부 (자기 자신 제외)
        string activeId = !string.IsNullOrEmpty(CurrentInspectingCellId) ? CurrentInspectingCellId : PhysicallyOpenedCellId;

        if (!string.IsNullOrEmpty(activeId) && activeId != cellId)
        {
            Debug.LogWarning($"[ISSM] 진입 거부: 이미 {activeId}번 방의 문이 열려있거나 점검 중입니다.");
            return false;
        }

        var cell = cellManager.GetCell(cellId);
        if (cell == null || !cell.IsActiveToday) return false;

        // 상태 변경 적용
        cell.IsInspectingNow = true;
        cell.State = CellState.Inspecting;
        CurrentInspectingCellId = cellId;
        _isSuppressionCleared = false;

        // 점검 진입 시 물리적 개방 상태도 함께 업데이트
        ReportPhysicalOpen(cellId);

        SetPrisonerState(cellId, pFsm => pFsm.OnStartInspection());

        OnEnteredCell?.Invoke(cellId);
        return true;
    }

    // =======================================================================
    // [2] 점검 종료 및 이탈
    // =======================================================================

    public void ForceReleaseOnTimeExpired()
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;
        var cellId = CurrentInspectingCellId;

        SetPrisonerState(cellId, pFsm => pFsm.BackToRoutine());
        cellManager.ForceReleaseInspectingOnly(cellId);
        EndInspection();
    }

    public void CompleteInspection(string cellId, bool didSuppress)
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null) return;

        OnResolved?.Invoke(cell.CellId, cell.IsSuspicious, didSuppress);
        cellManager.MarkResolvedAndLockForDay(cellId, didSuppress);

        SetPrisonerState(cellId, pFsm => pFsm.BackToRoutine());

        EndInspection();
    }

    public void EndInspection()
    {
        // 점검 종료 시 해당 ID에 대한 물리적 개방 보고도 해제
        if (!string.IsNullOrEmpty(CurrentInspectingCellId))
        {
            ReportPhysicalClose(CurrentInspectingCellId);
        }

        CurrentInspectingCellId = null;
        _isSuppressionCleared = false;
    }

    public bool RequestExitCell(string cellId) => true;

    // =======================================================================
    // [3] 진압 (Suppression)
    // =======================================================================
    public bool SelectSuppress(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null || cell.State == CellState.Suppressing) return false;

        cell.IsSuppressing = true;
        cell.State = CellState.Suppressing;

        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.CombatState));
        OnSuppressStarted?.Invoke(cellId);
        PrisonerEventBus.RaiseSuppressSessionStarted(cellId);

        return true;
    }

    private void HandlePrisonerDown(string downPrisonerInstanceId)
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;

        if (contentRegistry.TryGet(CurrentInspectingCellId, out var content))
        {
            if (content.prisonerInstanceId == downPrisonerInstanceId)
            {
                _isSuppressionCleared = true;
                NotifySuppressSuccess(CurrentInspectingCellId);
            }
        }
    }

    public bool NotifySuppressSuccess(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;

        cell.SuppressSuccess = true;
        OnSuppressSuccess?.Invoke(cellId);
        return true;
    }

    // =======================================================================
    // [4] 유틸리티
    // =======================================================================

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

    private CellRuntime GetCurrentCellOrNull(string cellId)
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId) || !CurrentInspectingCellId.Equals(cellId)) return null;
        return cellManager.GetCell(cellId);
    }
}