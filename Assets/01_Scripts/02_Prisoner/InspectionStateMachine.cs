using System;
using UnityEngine;

public class InspectionStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;

    public string CurrentInspectingCellId { get; private set; }

    // 외부(UI/시스템/정산) 연동 이벤트
    public event Action<string> OnEnteredCell;
    public event Action<string> OnExitBlocked;
    public event Action<string, bool, bool> OnResolved;
    public event Action<string> OnSuppressStarted;
    public event Action<string> OnSuppressSuccess;

    private bool _isSuppressionCleared; // 현재 방의 진압 완료 여부
    public bool IsSuppressionCleared => _isSuppressionCleared;

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        cellManager?.BuildCellsIfNeeded();
    }

    private void OnEnable()
    {
        // 죄수 사망 이벤트 구독
        PrisonerEventBus.OnPrisonerDown += HandlePrisonerDown;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
        PrisonerEventBus.OnPrisonerDown -= HandlePrisonerDown;
    }

    public bool TryEnterCell(string cellId)
    {
        if (cellManager == null) return false;

        // 1. 이미 다른 방을 점검 중이면 입장 불가
        if (!string.IsNullOrEmpty(CurrentInspectingCellId))
        {
            if (verboseLog()) Debug.LogWarning($"[ISSM] Already inspecting {CurrentInspectingCellId}. Cannot enter {cellId}.");
            return false;
        }

        var cell = cellManager.GetCell(cellId);
        if (cell == null) return false;

        // 2. 오늘 활성(소음) 방이거나 아직 해결되지 않은 방만 점검 가능
        if (!cell.IsActiveToday || cell.IsLockedForDay)
            return false;

        cell.IsInspectingNow = true;
        cell.State = CellState.Inspecting;
        CurrentInspectingCellId = cellId;

        // 입장 시 상태 초기화
        _isSuppressionCleared = false;

        OnEnteredCell?.Invoke(cellId);
        return true;
    }

    /// <summary>
    /// 문이 성공적으로 닫혔을 때 호출되어 시스템을 리셋합니다.
    /// 이 함수가 호출되어야 다음 방의 TryEnterCell이 성공합니다.
    /// </summary>
    public void EndInspection()
    {
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;

        Debug.Log($"[ISSM] Ending inspection for {CurrentInspectingCellId}. Resetting state.");

        // 점검 중인 ID와 상태 플래그 리셋
        CurrentInspectingCellId = null;
        _isSuppressionCleared = false;
    }

    public bool RequestExitCell(string cellId)
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null) return true;

        // ✅ 기획서 반영: 수상한 방(이상 현상 있음)인데 아직 죄수가 제압되지 않았다면 퇴장 불가
        if (cell.IsSuspicious && !_isSuppressionCleared)
        {
            Debug.LogWarning("[ISSM] Suspicious cell must be suppressed before exiting!");
            OnExitBlocked?.Invoke(cellId);
            return false;
        }

        // 정상 방이거나 진압이 완료된 경우 true 반환 (문 닫기 허용)
        return true;
    }

    private void HandlePrisonerDown(string instanceId)
    {
        // 현재 점검 중인 방의 죄수가 맞는지 확인
        if (!string.IsNullOrEmpty(CurrentInspectingCellId) && instanceId.StartsWith(CurrentInspectingCellId))
        {
            _isSuppressionCleared = true;

            // 데이터 매니저에 진압 성공 기록 (선택 사항: 문 닫을 때 해도 됨)
            NotifySuppressSuccess(CurrentInspectingCellId);

            Debug.Log($"[ISSM] Prisoner down in {CurrentInspectingCellId}. Suppression clear, exit allowed.");
        }
    }

    // --- 이하 기존 UI 연동 메서드 유지 ---

    public bool SelectWarning(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;
        if (cell.State == CellState.Suppressing || cell.IsSuppressing) return false;

        cell.NonSuppressChosen = true;
        return true;
    }

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

    public bool NotifySuppressSuccess(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null) return false;

        cell.SuppressSuccess = true;
        OnSuppressSuccess?.Invoke(cellId);
        return true;
    }

    private CellRuntime GetCurrentCellOrNull(string cellId)
    {
        if (cellManager == null) return null;
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return null;
        if (!string.Equals(CurrentInspectingCellId, cellId, StringComparison.Ordinal)) return null;
        return cellManager.GetCell(cellId);
    }

    public void CompleteInspection(string cellId, bool didSuppress)
    {
        var cell = cellManager.GetCell(cellId);
        if (cell == null) return;

        // 1. 리포트 빌더가 들을 수 있도록 이벤트 발생 (ResolvedRecord 생성 트리거)
        OnResolved?.Invoke(cell.CellId, cell.IsSuspicious, didSuppress);

        // 2. 데이터 업데이트 (잠금, 소음 OFF 등)
        cellManager.MarkResolvedAndLockForDay(cellId, didSuppress);

        // 3. 시스템 리셋 (다른 방 입장 가능하게)
        EndInspection();

        Debug.Log($"[ISSM] Inspection Complete recorded for {cellId}. Event Fired.");
    }

    private bool verboseLog() => true; // 디버그용
}