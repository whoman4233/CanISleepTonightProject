using System;
using UnityEngine;

public class InspectionStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonManager cellManager;
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
        if (cellManager == null) cellManager = FindObjectOfType<PrisonManager>();
        if (contentRegistry == null) contentRegistry = FindObjectOfType<CellContentRegistry>();
        cellManager?.BuildCellsIfNeeded();
    }

    private void OnEnable() => PrisonerEventBus.OnPrisonerDown += HandlePrisonerDown;
    private void OnDisable() => PrisonerEventBus.OnPrisonerDown -= HandlePrisonerDown;

    public bool TryEnterCell(string cellId)
    {
        // 1. 매니저 확인
        if (cellManager == null)
        {
            Debug.LogError($"[ISSM] {cellId}: CellManager가 연결되지 않았습니다.");
            return false;
        }

        // 2. 중복 점검 방지 (가장 유력한 원인)
        // 이미 다른 방(CurrentInspectingCellId)을 점검 중이라면 새로운 방을 열 수 없습니다.
        if (!string.IsNullOrEmpty(CurrentInspectingCellId))
        {
            Debug.LogWarning($"[ISSM] 진입 거부: 이미 '{CurrentInspectingCellId}'를 점검 중입니다. (요청된 방: {cellId})");
            return false;
        }

        var cell = cellManager.GetCell(cellId);

        // 3. 셀 존재 여부
        if (cell == null)
        {
            Debug.LogError($"[ISSM] CellManager에서 ID '{cellId}'를 찾을 수 없습니다.");
            return false;
        }

        // 4. 금일 활성화 여부 (IsActiveToday)
        if (!cell.IsActiveToday)
        {
            Debug.LogWarning($"[ISSM] {cellId}는 오늘 비활성화(IsActiveToday == false) 상태입니다.");
            return false;
        }

        // 5. 이미 잠긴 방인지 (IsLockedForDay)
        if (cell.IsLockedForDay)
        {
            Debug.LogWarning($"[ISSM] {cellId}는 이미 완료되어 잠긴(IsLockedForDay) 상태입니다.");
            return false;
        }

        // === 통과: 상태 변경 시작 ===
        cell.IsInspectingNow = true;
        cell.State = CellState.Inspecting;
        CurrentInspectingCellId = cellId;
        _isSuppressionCleared = false;

        // 죄수 FSM 상태 변경
        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.InspectionState));

        Debug.Log($"[ISSM] {cellId}: 점검 시작 승인 (CurrentInspectingCellId 갱신됨)");
        OnEnteredCell?.Invoke(cellId);
        return true;
    }

    public void ForceReleaseOnTimeExpired()
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;

        var cellId = CurrentInspectingCellId;

        // 죄수 상태 원복
        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.IdleState));

        // 매니저에게 강제 퇴거 알림
        cellManager.ForceReleaseInspectingOnly(cellId);

        // 내부 변수 초기화 (직접 null 대입 대신 EndInspection 활용)
        EndInspection();

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