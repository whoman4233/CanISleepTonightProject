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

    private bool _isSuppressionCleared; // 현재 방의 진압 완료 여부
    public bool IsSuppressionCleared => _isSuppressionCleared;

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

        _isSuppressionCleared = false; // 새 방에 들어갔으니 진압 상태 초기화
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

    // CellDoorInteractable이 문을 닫으려 할 때 호출하는 함수
    public bool RequestExitCell(string cellId)
    {
        // 기획서 반영: 수상한 방인데 아직 진압 플래그가 안 켜졌다면 false 반환
        var cell = cellManager.GetCell(cellId);
        if (cell.IsSuspicious && !_isSuppressionCleared)
        {
            Debug.LogWarning("아직 죄수가 제압되지 않아 문을 닫을 수 없습니다.");
            return false;
        }

        // 진압이 되었거나, 처음부터 정상이었던 방이면 true 반환
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
        // 정산 기록 이벤트(ReportBuilder가 받음)
        OnResolved?.Invoke(cell.CellId, cell.IsSuspicious, didSuppress);

        // ✅ 즉시 비활성화 X
        // ✅ 오늘은 잠금 + 소음 OFF + 해결 기록만 남김
        cellManager.MarkResolvedAndLockForDay(cell.CellId, didSuppress);

        // 점검 락 해제(다른 방 점검 가능)
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
        PrisonerEventBus.OnPrisonerDown += HandlePrisonerDown;
    }

    private void OnDisable()
    {
        PrisonerEventBus.OnPrisonerDown -= HandlePrisonerDown;
    }

    private void HandlePrisonerDown(string instanceId)
    {
        // 현재 점검 중인 방의 죄수가 맞는지 확인 후 플래그 ON
        // instanceId가 "C_1F_06_..." 형식이니 앞부분만 체크
        if (instanceId.StartsWith(CurrentInspectingCellId))
        {
            _isSuppressionCleared = true;
            Debug.Log($"[ISSM] {CurrentInspectingCellId} 진압 완료 확인. 퇴장 가능.");
        }
    }


}
