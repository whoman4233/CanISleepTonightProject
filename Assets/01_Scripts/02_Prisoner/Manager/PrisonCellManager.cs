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

    public IReadOnlyList<CellRuntime> Cells => _cells;
    public IReadOnlyList<CellRuntime> ActiveCells => _cells.Where(c => c.IsActiveToday).ToList();

    private readonly List<CellRuntime> _cells = new();
    private readonly Dictionary<string, CellRuntime> _byId = new();

    public event Action<string, bool> OnNoiseChanged; // (cellId, isNoisy)
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

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
                RunStandbySetup();
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

    public void RunStandbySetup(int? overrideActiveCount = null, int? overrideSuspiciousCount = null)
    {
        BuildCellsIfNeeded();

        int activeCount = overrideActiveCount ?? todayActiveCount;
        int suspiciousCount = overrideSuspiciousCount ?? todaySuspiciousCount;

        activeCount = Mathf.Clamp(activeCount, 0, _cells.Count);
        suspiciousCount = Mathf.Clamp(suspiciousCount, 0, activeCount);

        // Reset all
        foreach (var c in _cells)
            c.ResetForNewDay();

        // Pick active
        var shuffled = _cells.OrderBy(_ => UnityEngine.Random.value).ToList();
        var active = shuffled.Take(activeCount).ToList();

        foreach (var c in active)
        {
            c.IsActiveToday = true;
            SetNoisy(c, true);
            c.State = CellState.ActiveNoisy;
        }

        // Pick suspicious among active
        var suspicious = active.OrderBy(_ => UnityEngine.Random.value).Take(suspiciousCount).ToList();
        foreach (var c in suspicious)
            c.IsSuspicious = true;
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

        // 점검 완료 시 소음은 꺼짐(파동/Wave 제거 트리거)
        cell.IsNoisy = false;

        // 오늘 재입장 금지(문 잠금)
        cell.IsLockedForDay = true;

        cell.IsInspectingNow = false;
        cell.IsSuppressing = false;
        cell.State = CellState.LockedForDay;

        // 필요하면 여기서 OnNoiseChanged(cellId,false) 같은 이벤트도 발행
    }

}
