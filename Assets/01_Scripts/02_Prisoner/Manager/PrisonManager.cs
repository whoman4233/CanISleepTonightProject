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
        // 🔥 스케줄 매니저에게 오늘 데이터 요청 (이제 리스트가 아니라 데이터 조회 방식)
        // (주의: ScheduleManager가 리팩토링되면서 GetAssignmentsForDay 대신 다른 방식을 쓸 수도 있습니다.
        //  만약 GameFlowController가 직접 StartDay에서 뿌려주는 방식이라면 이 함수는 필요 없을 수도 있습니다.
        //  하지만 기존 구조 유지를 위해 ScheduleManager에서 데이터를 가져오는 형태로 작성합니다.)

        var scheduleMgr = PrisonerScheduleManager.Instance;
        if (scheduleMgr == null) return;

        Debug.Log($"[PrisonManager] {day}일차 감방 상태 초기화 중...");

        // 1. 기존 생성물 초기화
        if (spawnController != null) spawnController.ClearAllForNewDay();

        // 2. 논리적 상태 리셋
        ResetCellsForNewDay();
        ActiveCell1f = 0;
        ActiveCell2f = 0;

        // 3. 모든 셀을 돌며 "오늘 입주자인가?" 확인 후 처리
        foreach (var cellId in _runtimeCells.Keys)
        {
            var cellRuntime = _runtimeCells[cellId];
            var anchor = GetCellAnchor(cellId);
            if (anchor == null) continue;

            // 스케줄 매니저에게 "이 방에 누구 살고, 오늘 역할이 뭔지" 물어봄
            var prisonerData = scheduleMgr.GetPrisonerData(cellId);
            var dailyRole = scheduleMgr.GetDailyRole(cellId);

            // 입주민이 없으면 패스
            if (prisonerData == null) continue;

            // A. 논리 상태 업데이트
            cellRuntime.IsActiveToday = true;
            cellRuntime.IsSuspicious = dailyRole.isSuspicious; // 범인 여부
            cellRuntime.State = CellState.ActiveNoisy;
            SetNoisy(cellRuntime, true);

            if (cellRuntime.Floor == 1) ActiveCell1f++;
            else if (cellRuntime.Floor == 2) ActiveCell2f++;

            // B. 🔥 실제 소환 명령 (SpawnController에게 위임)
            // (SpawnController는 이미 ScheduleManager 데이터를 참조할 수 있으므로 cellId만 줘도 됨)
            if (spawnController != null)
            {
                spawnController.SpawnForCell(cellId, dailyRole.isSuspicious);
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