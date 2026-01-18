using System;
using UnityEngine;

public class InspectionStateMachine : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private CellContentRegistry contentRegistry;

    public string CurrentInspectingCellId { get; private set; }

    public event Action<string> OnEnteredCell;
    public event Action<string, bool, bool> OnResolved; // cellId, isSuspicious, didSuppress
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
    // [1] 점검 진입 (Enter)
    // =======================================================================
    public bool TryEnterCell(string cellId)
    {
        if (cellManager == null) return false;

        // 1. 중복 진입 방지
        // (다른 방을 점검 중일 때만 진입을 막음)
        if (!string.IsNullOrEmpty(CurrentInspectingCellId) && CurrentInspectingCellId != cellId)
        {
            Debug.LogWarning($"[ISSM] 진입 거부: 이미 {CurrentInspectingCellId} 점검 중.");
            return false;
        }

        var cell = cellManager.GetCell(cellId);
        if (cell == null) return false;

        // 2. 상태 체크 (오늘 활성 여부)
        if (!cell.IsActiveToday) return false;

        // 3. 상태 변경 적용
        cell.IsInspectingNow = true;
        cell.State = CellState.Inspecting;
        CurrentInspectingCellId = cellId;
        _isSuppressionCleared = false;

        // 4. 죄수 상태 변경 명령
        //    무조건 InspectionState로 바꾸지 않고, FSM에게 '점검 시작' 신호만 보냄
        //    (FSM 내부에서 AIType에 따라 순응할지, 무시할지, 도망갈지 결정)
        SetPrisonerState(cellId, pFsm => pFsm.OnStartInspection());

        OnEnteredCell?.Invoke(cellId);
        return true;
    }

    // =======================================================================
    // [2] 점검 종료 및 이탈 (Exit / Complete)
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

        //FSM의 분기 함수 호출 (일반인은 Return, 특수인은 CenterIdle로 자동 분기)
        SetPrisonerState(cellId, pFsm =>
        {
            Debug.Log($"[ISSM] {cellId} 점검 종료 -> 복귀 루틴 실행(BackToRoutine)");
            pFsm.BackToRoutine();
        });

        EndInspection();
    }

    // 내부 초기화
    public void EndInspection()
    {
        CurrentInspectingCellId = null;
        _isSuppressionCleared = false;
    }

    // 문 닫기 요청 (UI 버튼 등)
    public bool RequestExitCell(string cellId)
    {
        // 문 닫기 애니메이션 등을 기다려야 한다면 여기서 false 리턴 로직 추가 가능
        return true;
    }

    // =======================================================================
    // [3] 진압 (Suppression)
    // =======================================================================
    public bool SelectSuppress(string cellId)
    {
        var cell = GetCurrentCellOrNull(cellId);
        if (cell == null || cell.State == CellState.Suppressing) return false;

        cell.IsSuppressing = true;
        cell.State = CellState.Suppressing;

        // 죄수를 전투 상태로 전환
        SetPrisonerState(cellId, pFsm => pFsm.ChangeState(pFsm.CombatState));

        OnSuppressStarted?.Invoke(cellId);

        // (선택) PrisonerEventBus를 통해서도 알림
        PrisonerEventBus.RaiseSuppressSessionStarted(cellId);

        return true;
    }

    // 죄수가 쓰러졌을 때 호출되는 콜백
    private void HandlePrisonerDown(string downPrisonerInstanceId)
    {
        // 현재 점검 중이 아니면 패스
        if (string.IsNullOrEmpty(CurrentInspectingCellId)) return;

        // 레지스트리에서 현재 방의 죄수 ID를 직접 확인
        if (contentRegistry.TryGet(CurrentInspectingCellId, out var content))
        {
            // 쓰러진 죄수가 지금 내 눈앞에 있는 죄수가 맞는가?
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

    // 죄수 FSM 제어 헬퍼 (Registry 활용)
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