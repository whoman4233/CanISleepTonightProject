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

    // [삭제됨] _activePrisoners 리스트는 이제 사용하지 않음 (SpawnController가 관리)

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

    private void OnEnable() => EventBus.Subscribe(_onPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onPhaseChanged);

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

        // =================================================================
        // [2] 미션 및 데이터 준비 (Mission Strategy Execution)
        // =================================================================
        // ★ 여기서 PrisonManager가 직접 배정하지 않고, MissionManager에게 위임합니다.
        
        var missionMgr = DailyMissionManager.Instance; // 혹은 [SerializeField] 참조
        
        if (missionMgr != null)
        {
            // 이 함수 내부에서 오늘 날짜의 Strategy를 찾고, 
            // Strategy.SetupDay()가 호출되면서 --> ScheduleManager.AssignRoles()가 실행됨
            missionMgr.StartDay(day);
            
            if (verboseLog) Debug.Log($"[PrisonManager] 미션 매니저를 통해 {day}일차 전략(Strategy)이 적용되었습니다.");
        }
        else
        {
            // 미션 매니저가 없을 경우를 대비한 비상용 기본 배정
            Debug.LogWarning("[PrisonManager] DailyMissionManager가 없습니다. 기본값으로 배정합니다.");
            PrisonerScheduleManager.Instance?.AssignRolesForNewDay(1, PrisonerAIType.Good);
        }

        // =================================================================
        // [3] 소환 및 활성화 (Spawning based on Assigned Roles)
        // =================================================================
        // 위 [2]번 단계에서 역할 배정이 DB에 저장되었으므로, 이제 데이터를 읽어서 소환만 하면 됨
        
        var scheduleMgr = PrisonerScheduleManager.Instance;

        foreach (var cellId in _runtimeCells.Keys)
        {
            var cellRuntime = _runtimeCells[cellId];
            var anchor = GetCellAnchor(cellId);
            if (anchor == null) continue;

            // 스케줄러에서 데이터 조회 (방금 전략에 의해 배정된 데이터)
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

            // B. 실제 소환 (SpawnController는 결정된 데이터(스킨 포함)를 보고 프리팹 생성)
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

    // 감방 문제가 해결되었을 때 (비활성화)
    public void ResolveAndDeactivateCell(string cellId)
    {
        var cell = GetCellRuntime(cellId);
        if (cell == null) return;

        cell.IsActiveToday = false;
        SetNoisy(cell, false);
        cell.State = CellState.Inactive;
    }

    // 하루 동안 잠금 처리 (제압/완료 등)
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

    // 활성화된 방 ID 목록 반환 (순찰이나 랜덤 이벤트용)
    public List<string> GetActiveCellIds()
    {
        return _runtimeCells.Values
            .Where(c => c.IsActiveToday)
            .Select(c => c.CellId)
            .ToList();
    }

    // 외부 호환성 (GetCellRuntime alias)
    public CellRuntime GetCell(string cellId) => GetCellRuntime(cellId);
}