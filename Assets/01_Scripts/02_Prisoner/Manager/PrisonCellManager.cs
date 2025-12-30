using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonCellManager : MonoBehaviour
{
    [Header("Grid Config")]
    [SerializeField] private int floors = 2;
    [SerializeField] private int cellsPerFloor = 8;

    [Header("Standby Config")]
    [SerializeField] private int todayActiveCount = 6;
    [SerializeField] private int todaySuspiciousCount = 3;

    [Header("Debug")]
    [SerializeField] private bool autoBuildOnAwake = true;
    [SerializeField] private bool verboseLog = false;

    public IReadOnlyList<CellRuntime> Cells => _cells;
    public IReadOnlyList<CellRuntime> ActiveCells => _cells.Where(c => c.IsActiveToday).ToList();

    private readonly List<CellRuntime> _cells = new();
    private readonly Dictionary<string, CellRuntime> _byId = new();

    public event Action<string, bool> OnNoiseChanged; // (cellId, isNoisy)
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    //현재 활성화 된 1층,2층 감방 수
    public int ActiveCell1f = 0;
    public int ActiveCell2f = 0;

    private void Awake()
    {
        if (autoBuildOnAwake)
        {
            BuildCellsIfNeeded();
        }
        _onPhaseChanged = e =>
        {
            if (e.Phase == GamePhase.Standby)
            {
                Debug.Log("PrisonCellManager의 BuildCellsIfNeeded 완료");
            }
        };

    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    public void BuildCellsIfNeeded()
    {
        if (_cells.Count > 0) return;

        _cells.Clear();
        _byId.Clear();

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
                    State = CellState.Inactive
                };
                _cells.Add(cell);
                _byId[id] = cell;
            }
        }
    }

    public static string MakeCellId(int floor, int number)
    {
        // C_1F_01, C_2F_08
        return $"C_{floor}F_{number:00}";
    }

    public CellRuntime GetCell(string cellId)
    {
        if (string.IsNullOrWhiteSpace(cellId)) return null;
        return _byId.TryGetValue(cellId, out var cell) ? cell : null;
    }

    public void ApplyDailySchedule(Dictionary<string, bool> assignments)
    {
        BuildCellsIfNeeded();

        // 1. 모든 방 초기화
        foreach (var c in _cells)
            c.ResetForNewDay();

        // 2. 카운트 초기화
        ActiveCell1f = 0;
        ActiveCell2f = 0;

        // 3. 스케줄대로 적용
        foreach (var kvp in assignments)
        {
            string cellId = kvp.Key;
            bool isSuspicious = kvp.Value;

            var cell = GetCell(cellId);
            if (cell == null) continue;

            // 활성 상태 설정
            cell.IsActiveToday = true;
            cell.IsSuspicious = isSuspicious;
            SetNoisy(cell, true);
            cell.State = CellState.ActiveNoisy;

            // 층별 카운트 집계
            if (cell.Floor == 1) ActiveCell1f++;
            else if (cell.Floor == 2) ActiveCell2f++;
        }

        if (verboseLog) Debug.Log($"[CellManager] Schedule Applied. 1F: {ActiveCell1f}, 2F: {ActiveCell2f}");
    }

    public void ResolveAndDeactivateCell(string cellId)
    {
        var cell = GetCell(cellId);
        if (cell == null) return;

        cell.IsActiveToday = false;
        SetNoisy(cell, false);

        cell.IsInspectingNow = false;
        cell.IsSuppressing = false;
        cell.State = CellState.Inactive; // "오늘 처리 끝"이므로 비활성으로 내려버림
    }

    public void ForceReleaseInspectingOnly(string cellId)
    {
        // 시간초과 규칙: Exit 호출 X, 완료처리 X.
        // Inspecting/Suppressing 락만 풀고 ActiveNoisy 유지.
        var cell = GetCell(cellId);
        if (cell == null) return;

        cell.IsInspectingNow = false;
        cell.IsSuppressing = false;
        cell.SuppressSuccess = false;

        if (cell.IsActiveToday)
            cell.State = CellState.ActiveNoisy;
        else
            cell.State = CellState.Inactive;
    }

    public List<string> GetActiveCellIds()
    {
        return _cells.Where(c => c.IsActiveToday).Select(c => c.CellId).ToList();
    }

    private void SetNoisy(CellRuntime cell, bool noisy)
    {
        if (cell.IsNoisy == noisy) return;
        cell.IsNoisy = noisy;
        OnNoiseChanged?.Invoke(cell.CellId, noisy);
    }

    public void MarkResolvedAndLockForDay(string cellId, bool didSuppress)
    {
        var cell = GetCell(cellId);
        if (cell == null) return;

        cell.WasResolvedToday = true;
        cell.DidSuppress = didSuppress;
        SetNoisy(cell, false);

        // 오늘 재입장 금지 및 상태 변경
        cell.IsLockedForDay = true;
        cell.IsInspectingNow = false;
        cell.IsSuppressing = false;
        cell.State = CellState.LockedForDay;

        if (verboseLog) Debug.Log($"[CellManager] Cell {cellId} resolved. Locked for the day.");
    }

}