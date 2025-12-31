using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonManager : MonoBehaviour
{
    [Header("References")]
    // 🔥 [추가] 생성 담당자 연결
    [SerializeField] private PrisonerSpawnController spawnController;

    // [삭제] 직접 생성 안 하므로 프리팹/DB 필요 없음 (SpawnController가 가짐)
    // [SerializeField] private GameObject prisonerPrefab; 
    // [SerializeField] private PrisonerDatabaseSO prisonerDatabase;

    [SerializeField] private List<CellAnchor> cellAnchors;

    [Header("Grid Config")]
    [SerializeField] private int floors = 2;
    [SerializeField] private int cellsPerFloor = 8;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    // 내부 데이터
    private readonly Dictionary<string, CellRuntime> _runtimeCells = new();
    private Dictionary<string, CellAnchor> _anchorMap = new();

    // [참고] 이제 ActivePrisoners 리스트는 SpawnController(Registry)가 관리하므로
    // PrisonManager에서는 직접적인 리스트 관리가 필요 없어졌습니다.
    // 하지만 외부에서 접근하는 코드가 있을 수 있으므로 빈 리스트로 남겨두거나, 
    // 필요하다면 Registry에서 조회하도록 구조를 바꿔야 합니다. 
    // 여기서는 일단 컴파일 에러 방지를 위해 남겨두지만, 실제로는 비어있게 됩니다.
    private List<PrisonerController> _activePrisoners = new List<PrisonerController>();
    public IReadOnlyList<PrisonerController> ActivePrisoners => _activePrisoners;

    public event Action<string, bool> OnNoiseChanged;

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private int _lastLoadedDay = -1;

    public int ActiveCell1f { get; private set; }
    public int ActiveCell2f { get; private set; }

    private void Awake()
    {
        // 앵커 자동 할당
        if (cellAnchors == null || cellAnchors.Count == 0)
        {
            cellAnchors = FindObjectsOfType<CellAnchor>().ToList();
            if (verboseLog) Debug.Log($"[PrisonManager] 앵커 {cellAnchors.Count}개 자동 할당됨.");
        }

        // 1. 물리 앵커 맵핑
        _anchorMap.Clear();
        foreach (var anchor in cellAnchors)
        {
            if (anchor != null)
            {
                if (!string.IsNullOrEmpty(anchor.cellId))
                    _anchorMap[anchor.cellId] = anchor;
                else
                    Debug.LogWarning($"[PrisonManager] {anchor.name}의 CellID가 비어있습니다.");
            }
        }

        // 2. 논리 셀 데이터 구축
        BuildRuntimeCells();

        // 🔥 스폰 컨트롤러 찾기
        if (spawnController == null) spawnController = FindObjectOfType<PrisonerSpawnController>();

        _onPhaseChanged = HandleGamePhaseChanged;
    }

    private void OnEnable() => EventBus.Subscribe(_onPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onPhaseChanged);

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.Phase == GamePhase.Standby)
        {
            int currentDay = (GameManager.Instance != null) ? GameManager.Instance.CurrentDay : 1;
            if (currentDay <= 0) currentDay = 1;

            if (_lastLoadedDay == currentDay)
            {
                // 이미 로드된 날짜라면 스킵 (단, 강제 리로드 필요시 로직 수정 가능)
                if (verboseLog) Debug.Log($"[PrisonManager] {currentDay}일차 스케줄은 이미 로드되었습니다.");
                return;
            }

            _lastLoadedDay = currentDay;
            LoadAndApplyTodaySchedule(currentDay);
        }
    }

    private void LoadAndApplyTodaySchedule(int day)
    {
        var scheduleMgr = FindObjectOfType<PrisonerScheduleManager>();
        if (scheduleMgr == null) return;

        Debug.Log($"[PrisonManager] {day}일차 스탠바이 진입 -> 스케줄 로드 시작");

        var todaysAssignments = scheduleMgr.GetAssignmentsForDay(day);

        if (todaysAssignments != null && todaysAssignments.Count > 0)
        {
            ApplyDailySchedule(todaysAssignments);
        }
        else
        {
            Debug.LogError($"[PrisonManager] 스케줄 로드 실패 (Day: {day})");
        }
    }

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

    public static string MakeCellId(int floor, int number) => $"C_{floor}F_{number:00}";
    public CellRuntime GetCellRuntime(string cellId) => _runtimeCells.TryGetValue(cellId, out var cell) ? cell : null;
    public CellAnchor GetCellAnchor(string cellId) => _anchorMap.TryGetValue(cellId, out var anchor) ? anchor : null;

    // -------------------------------------------------------------
    // 스케줄 적용 (핵심)
    // -------------------------------------------------------------
    public void ApplyDailySchedule(Dictionary<string, DailyCellAssignment> assignments)
    {
        // 1. 기존 생성물 초기화 (SpawnController에게 위임)
        if (spawnController != null)
        {
            spawnController.ClearAllForNewDay();
        }

        // 2. 논리적 상태 초기화
        ResetCellsForNewDay();

        ActiveCell1f = 0;
        ActiveCell2f = 0;

        foreach (var kvp in assignments)
        {
            string cellId = kvp.Key;
            DailyCellAssignment info = kvp.Value;

            var cellRuntime = GetCellRuntime(cellId);

            // Anchor가 없으면 생성 불가
            if (cellRuntime == null || GetCellAnchor(cellId) == null) continue;

            // 3. 방 상태 설정 (논리)
            cellRuntime.IsActiveToday = true;
            cellRuntime.IsSuspicious = info.isSuspicious;
            SetNoisy(cellRuntime, true);
            cellRuntime.State = CellState.ActiveNoisy;

            if (cellRuntime.Floor == 1) ActiveCell1f++;
            else if (cellRuntime.Floor == 2) ActiveCell2f++;

            // 4. 🔥 [핵심] 실제 생성은 스폰 컨트롤러에게 명령!
            if (spawnController != null)
            {
                spawnController.SpawnForCell(cellId, info.isSuspicious);
            }
        }

        if (verboseLog) Debug.Log($"[PrisonManager] Day Started. Active: {assignments.Count}");
    }

    // -------------------------------------------------------------
    // 🔥 [삭제됨] 생성 관련 로직은 모두 SpawnController로 이동했습니다.
    // -------------------------------------------------------------
    // SpawnAnomaliesInCell, ReplaceObject, SpawnAtRandomSlot, 
    // ClearAllAnomalies, SpawnPrisonerInCell, DespawnAllPrisoners 삭제됨.

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

        if (verboseLog) Debug.Log($"[PrisonManager] Cell {cellId} resolved.");
    }

    private void SetNoisy(CellRuntime cell, bool noisy)
    {
        if (cell.IsNoisy == noisy) return;
        cell.IsNoisy = noisy;
        OnNoiseChanged?.Invoke(cell.CellId, noisy);
    }

    public void BuildCellsIfNeeded()
    {
        if (_runtimeCells.Count > 0) return;
        BuildRuntimeCells();
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

    public List<string> GetActiveCellIds()
    {
        var list = new List<string>();
        foreach (var cell in _runtimeCells.Values)
        {
            if (cell.IsActiveToday) list.Add(cell.CellId);
        }
        return list;
    }

    public CellRuntime GetCell(string cellId) => GetCellRuntime(cellId);
}