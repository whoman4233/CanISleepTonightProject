using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<CellAnchor> cellAnchors;
    // 🔥 [변경] 생성/관리 담당자 연결
    [SerializeField] private PrisonerSpawnController spawnController;

    [Header("Grid Config")]
    [SerializeField] private int floors = 2;
    [SerializeField] private int cellsPerFloor = 8;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    // 내부 데이터 (논리적 셀 상태)
    private readonly Dictionary<string, CellRuntime> _runtimeCells = new();
    private Dictionary<string, CellAnchor> _anchorMap = new();

    // 이벤트
    public event Action<string, bool> OnNoiseChanged;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private int _lastLoadedDay = -1;

    // UI 표시용 카운트
    public int ActiveCell1f { get; private set; }
    public int ActiveCell2f { get; private set; }

    private void Awake()
    {
        // 1. 앵커 자동 할당
        if (cellAnchors == null || cellAnchors.Count == 0)
        {
            cellAnchors = FindObjectsOfType<CellAnchor>().ToList();
            if (verboseLog) Debug.Log($"[PrisonManager] 앵커 {cellAnchors.Count}개 자동 할당됨.");
        }

        // 2. 물리 앵커 맵핑 (ID -> Anchor)
        _anchorMap.Clear();
        foreach (var anchor in cellAnchors)
        {
            if (anchor != null && !string.IsNullOrEmpty(anchor.cellId))
            {
                _anchorMap[anchor.cellId] = anchor;
            }
        }

        // 3. 논리 셀 데이터 구축 (CellRuntime 생성)
        BuildRuntimeCells();

        // 4. 스폰 컨트롤러 찾기 (없으면 경고)
        if (spawnController == null)
        {
            spawnController = FindObjectOfType<PrisonerSpawnController>();
            if (spawnController == null) Debug.LogError("[PrisonManager] PrisonerSpawnController가 씬에 없습니다!");
        }

        _onPhaseChanged = HandleGamePhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);

        // ★ [추가] 죄수 제압(Down) 이벤트 구독
        PrisonerEventBus.OnPrisonerDown += HandlePrisonerDown;
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);

        // ★ [추가] 구독 해제
        PrisonerEventBus.OnPrisonerDown -= HandlePrisonerDown;
    }

    // ★ [핵심 로직] 죄수가 쓰러졌을 때 호출됨 (중계 역할)
    private void HandlePrisonerDown(string prisonerId)
    {
        // 1. 누가 죽었는지 식별 (ID -> CellID 변환)
        // (PrisonerScheduleManager에 추가한 GetCellIdByPrisonerId 함수 사용)
        string cellId = PrisonerScheduleManager.Instance.GetCellIdByPrisonerId(prisonerId);

        if (string.IsNullOrEmpty(cellId))
        {
            if (verboseLog) Debug.LogWarning($"[PrisonManager] 쓰러진 죄수({prisonerId})의 소속 방을 찾을 수 없습니다.");
            return;
        }

        if (verboseLog) Debug.Log($"[PrisonManager] 죄수 제압 확인: {prisonerId} (소속: {cellId})");

        // 2. 미션 매니저에게 신고 (점수 획득 or 진짜 프랭크 잡았으면 실패 처리)
        if (DailyMissionManager.Instance != null)
        {
            // 이 함수가 호출되어야 Mission Strategy의 IsValidPrisoner()가 실행됨
            DailyMissionManager.Instance.NotifyPrisonerResolved(cellId);
        }

        // 3. 해당 방 해결 처리 (잠금)
        // - 미션 성공/실패 여부와 상관없이, 제압된 방은 '완료' 상태로 변경
        MarkResolvedAndLockForDay(cellId, true);
    }

    // =======================================================================
    // [1] 게임 페이즈 관리 (하루 시작 감지)
    // =======================================================================

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        // Standby 페이즈 진입 시 = 하루가 시작될 때
        if (evt.Phase == GamePhase.Standby)
        {
            int currentDay = (GameManager.Instance != null) ? GameManager.Instance.CurrentDay : 1;
            if (currentDay <= 0) currentDay = 1;

            // 중복 로드 방지
            if (_lastLoadedDay == currentDay) return;

            _lastLoadedDay = currentDay;
            LoadAndApplyTodaySchedule(currentDay);
        }
    }

    private void LoadAndApplyTodaySchedule(int day)
    {
        Debug.Log($"[PrisonManager] {day}일차 하루 일과 시작 시퀀스 가동");

        // =================================================================
        // [1] 사전 청소 (Clean Up)
        // =================================================================
        if (spawnController != null) spawnController.ClearAllForNewDay();
        ResetCellsForNewDay();
        ActiveCell1f = 0;
        ActiveCell2f = 0;

        // ============================================================
        // 하루 시작 시점의 "일일 데이터" 초기화
        // - 역할/일일 플래그가 꼬이면 "안 죽음" 같은 증상이 발생할 수 있음
        // ============================================================
        if (GameManager.Instance != null)
        {
            var save = new SaveManager().LoadGame();

            // 새 하루 진입인 경우만 일일 상태 초기화
            if (save == null || save.isMissionInProgress == false)
            {
                PrisonerScheduleManager.Instance?.ResetDailyState();
            }
            else
            {
                if (verboseLog)
                    Debug.Log("[PrisonManager] 이어하기 → ResetDailyState 스킵");
            }
        }

        // =================================================================
        // [2] 미션 및 데이터 준비 (Mission Strategy Execution)
        // =================================================================

        var missionMgr = DailyMissionManager.Instance;

        if (missionMgr == null || !missionMgr.HasValidRunMissionTable)
        {
            Debug.LogError("[PrisonManager] MissionTable 준비 안 됨 → StartDay 중단");
            return;
        }
        if (missionMgr != null)
        {
            missionMgr.StartDay(day);
            if (verboseLog) Debug.Log($"[PrisonManager] 미션 매니저를 통해 {day}일차 전략(Strategy)이 적용되었습니다.");
        }
        else
        {
            Debug.LogWarning("[PrisonManager] DailyMissionManager가 없습니다. 기본값으로 배정합니다.");
            PrisonerScheduleManager.Instance?.AssignRolesForNewDay(1, PrisonerAIType.Good);
        }

        // =================================================================
        // [3] 소환 및 활성화 (Spawning based on Assigned Roles)
        // =================================================================

        var scheduleMgr = PrisonerScheduleManager.Instance;

        foreach (var cellId in _runtimeCells.Keys)
        {
            var cellRuntime = _runtimeCells[cellId];
            var anchor = GetCellAnchor(cellId);
            if (anchor == null) continue;

            var prisonerData = scheduleMgr?.GetPrisonerData(cellId);
            var dailyRole = scheduleMgr?.GetDailyRole(cellId);

            if (prisonerData == null) continue;

            // A. 논리 상태 업데이트
            cellRuntime.IsActiveToday = true;
            cellRuntime.IsSuspicious = dailyRole.HasValue && dailyRole.Value.isSuspicious;
            cellRuntime.State = CellState.ActiveNoisy;
            SetNoisy(cellRuntime, true);

            if (cellRuntime.Floor == 1) ActiveCell1f++;
            else if (cellRuntime.Floor == 2) ActiveCell2f++;

            // B. 실제 소환
            if (spawnController != null && dailyRole.HasValue)
            {
                spawnController.SpawnForCell(cellId, dailyRole.Value.isSuspicious);
            }
        }

        if (verboseLog) Debug.Log($"[PrisonManager] Day {day} Started. Active Cells: {ActiveCell1f + ActiveCell2f}");
    }

    // =======================================================================
    // [2] 셀 논리 관리 (Runtime Logic)
    // =======================================================================

    private void BuildRuntimeCells()
    {
        _runtimeCells.Clear();
        for (int f = 1; f <= floors; f++)
        {
            for (int n = 1; n <= cellsPerFloor; n++)
            {
                var id = MakeCellId(f, n);
                var cell = new CellRuntime
                {
                    CellId = id,
                    Floor = f,
                    Number = n,
                    IsActiveToday = false,
                    State = CellState.Inactive
                };
                _runtimeCells[id] = cell;
            }
        }
    }

    private void ResetCellsForNewDay()
    {
        foreach (var c in _runtimeCells.Values)
            c.ResetForNewDay();
    }

    public void ResolveAndDeactivateCell(string cellId)
    {
        var cell = GetCellRuntime(cellId);
        if (cell == null) return;

        cell.IsActiveToday = false;
        SetNoisy(cell, false);
        cell.State = CellState.Inactive;
    }

    public void MarkResolvedAndLockForDay(string cellId, bool didSuppress)
    {
        var cell = GetCellRuntime(cellId);
        if (cell == null) return;

        cell.WasResolvedToday = true;
        cell.DidSuppress = didSuppress;
        SetNoisy(cell, false);
        cell.IsLockedForDay = true;
        cell.State = CellState.LockedForDay;

        if (verboseLog) Debug.Log($"[PrisonManager] Cell {cellId} resolved (Lock).");
    }

    private void SetNoisy(CellRuntime cell, bool noisy)
    {
        if (cell.IsNoisy == noisy) return;
        cell.IsNoisy = noisy;
        OnNoiseChanged?.Invoke(cell.CellId, noisy);
    }

    public void ForceReleaseInspectingOnly(string cellId)
    {
        var cell = GetCellRuntime(cellId);
        if (cell == null) return;

        cell.IsInspectingNow = false;
        cell.IsSuppressing = false;
        cell.SuppressSuccess = false;

        if (cell.IsActiveToday) cell.State = CellState.ActiveNoisy;
        else cell.State = CellState.Inactive;
    }

    // =======================================================================
    // [3] 유틸리티
    // =======================================================================

    public static string MakeCellId(int floor, int number) => $"C_{floor}F_{number:00}";

    public CellRuntime GetCellRuntime(string cellId) => _runtimeCells.TryGetValue(cellId, out var cell) ? cell : null;

    public CellAnchor GetCellAnchor(string cellId) => _anchorMap.TryGetValue(cellId, out var anchor) ? anchor : null;

    public List<string> GetActiveCellIds()
    {
        return _runtimeCells.Values
            .Where(c => c.IsActiveToday)
            .Select(c => c.CellId)
            .ToList();
    }

    public CellRuntime GetCell(string cellId) => GetCellRuntime(cellId);
}