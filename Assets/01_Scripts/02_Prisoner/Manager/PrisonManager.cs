using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;

    [SerializeField] private List<CellAnchor> cellAnchors;

    [Header("Grid Config")]
    [SerializeField] private int floors = 2;
    [SerializeField] private int cellsPerFloor = 8;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    // 내부 데이터
    private readonly Dictionary<string, CellRuntime> _runtimeCells = new();
    private Dictionary<string, CellAnchor> _anchorMap = new();
    private List<PrisonerController> _activePrisoners = new List<PrisonerController>();

    public IReadOnlyList<PrisonerController> ActivePrisoners => _activePrisoners;

    public event Action<string, bool> OnNoiseChanged;

    public int ActiveCell1f { get; private set; }
    public int ActiveCell2f { get; private set; }

    private void Awake()
    {
        foreach (var anchor in cellAnchors)
        {
            if (anchor != null)
                _anchorMap[anchor.cellId] = anchor;
        }
        BuildRuntimeCells();
    }

    // [테스트용 Start 추가] 바로 테스트하고 싶으시면 이 주석을 푸세요.
    /*
    private void Start()
    {
        // 모든 방 활성화 테스트
        var testAssignments = new Dictionary<string, DailyCellAssignment>();
        foreach(var key in _runtimeCells.Keys) 
        {
            testAssignments[key] = new DailyCellAssignment { 
                isSuspicious = false, dailyAIType = PrisonerAIType.Normal 
            };
        }
        ApplyDailySchedule(testAssignments);
    }
    */

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
                    IsActiveToday = false, // 기본값 false (스케줄에서 true로 변경됨)
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
    // 스케줄 적용
    // -------------------------------------------------------------
    public void ApplyDailySchedule(Dictionary<string, DailyCellAssignment> assignments)
    {
        DespawnAllPrisoners();
        ResetCellsForNewDay();
        ClearAllAnomalies(); // 기존 이상현상 및 가구 복구

        ActiveCell1f = 0;
        ActiveCell2f = 0;

        foreach (var kvp in assignments)
        {
            string cellId = kvp.Key;
            DailyCellAssignment info = kvp.Value;

            var cellRuntime = GetCellRuntime(cellId);
            var cellAnchor = GetCellAnchor(cellId);

            if (cellRuntime == null || cellAnchor == null) continue;

            // 1. 방 상태 설정
            cellRuntime.IsActiveToday = true;
            cellRuntime.IsSuspicious = info.isSuspicious;
            SetNoisy(cellRuntime, true);
            cellRuntime.State = CellState.ActiveNoisy;

            if (cellRuntime.Floor == 1) ActiveCell1f++;
            else if (cellRuntime.Floor == 2) ActiveCell2f++;

            // 2. 죄수 스폰
            SpawnPrisonerInCell(cellAnchor, info);

            // 3. 이상현상 스폰 (핵심 로직)
            SpawnAnomaliesInCell(cellId, cellAnchor, info.isSuspicious);
        }

        if (verboseLog) Debug.Log($"[PrisonManager] Day Started. Active: {assignments.Count}");
    }

    // -------------------------------------------------------------
    // [핵심] 이상현상 스폰 & 교체 로직
    // -------------------------------------------------------------
    private void SpawnAnomaliesInCell(string cellId, CellAnchor anchor, bool isSuspicious)
    {
        if (anchor.currentDailyAnomalies == null) return;

        // 1. 범인 지목
        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious && anchor.currentDailyAnomalies.Count > 0)
        {
            int rndIndex = UnityEngine.Random.Range(0, anchor.currentDailyAnomalies.Count);
            culpritDef = anchor.currentDailyAnomalies[rndIndex];
        }

        // 2. 리스트 순회
        foreach (var def in anchor.currentDailyAnomalies)
        {
            bool isRealAnomaly = (def == culpritDef);

            // CASE A: 교체형 (침대, 벽 등) -> 성능 최적화 (진짜일 때만 교체)
            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (isRealAnomaly)
                {
                    ReplaceObject(cellId, anchor, def, def.suspiciousPrefab, true);
                }
            }
            // CASE B: 슬롯형 (시계 등) -> 진짜거나, '항상 보여줘야 하는' 경우 생성
            else
            {
                if (isRealAnomaly || def.alwaysSpawnNormal)
                {
                    GameObject prefab = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;
                    SpawnAtRandomSlot(cellId, anchor, def, prefab, isRealAnomaly);
                }
            }
        }
    }

    // [수정됨] Switch문 삭제! Structure에서 바로 가져옵니다.
    private void ReplaceObject(string cellId, CellAnchor anchor, AnomalyDefinitionSO def, GameObject prefab, bool isReal)
    {
        if (prefab == null) return;
        if (anchor.structure == null) return;

        // 🔥 CellStructure에게 "이 타입에 해당하는 기본 오브젝트 줘" 라고 요청
        GameObject targetObj = anchor.structure.GetDefaultObject(def.targetType);

        if (targetObj != null)
        {
            targetObj.SetActive(false); // 기존 끄기

            // 새 프리팹 생성 (부모 유지)
            var go = Instantiate(prefab, targetObj.transform.position, targetObj.transform.rotation, targetObj.transform.parent);

            var actor = go.GetComponent<AnomalyActor>();
            if (actor != null) actor.Init(cellId, def, isReal);
        }
    }

    private void SpawnAtRandomSlot(string cellId, CellAnchor anchor, AnomalyDefinitionSO def, GameObject prefab, bool isReal)
    {
        if (prefab == null) return;
        if (anchor.anomalySlots == null || anchor.anomalySlots.Count == 0) return;

        // 랜덤 슬롯 선택
        var targetSlot = anchor.anomalySlots[UnityEngine.Random.Range(0, anchor.anomalySlots.Count)];

        // 🔴 [수정] targetSlot 뒤에 .transform 을 붙여야 합니다!
        var go = Instantiate(prefab, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);

        var actor = go.GetComponent<AnomalyActor>();
        if (actor != null) actor.Init(cellId, def, isReal);
    }

    // [수정됨] 청소 및 복구 로직
    private void ClearAllAnomalies()
    {
        foreach (var anchor in _anchorMap.Values)
        {
            // 1. 생성된 이상현상 제거
            var anomalies = anchor.GetComponentsInChildren<AnomalyActor>(true);
            foreach (var a in anomalies) Destroy(a.gameObject);

            // 2. 꺼뒀던 기본 가구들 일괄 복구 (Structure 이용)
            if (anchor.structure != null)
            {
                anchor.structure.ResetAllDefaults();
            }
        }
    }

    // -------------------------------------------------------------
    // 죄수 및 기타 로직 (기존 유지)
    // -------------------------------------------------------------
    private void SpawnPrisonerInCell(CellAnchor anchor, DailyCellAssignment info)
    {
        var scheduleMgr = FindObjectOfType<PrisonerScheduleManager>();
        // 혹시 스케줄 매니저 없으면 null 처리
        var def = scheduleMgr != null ? scheduleMgr.GetAssignedPrisonerDef(anchor.cellId) : null;

        if (def != null)
        {
            PrisonerData newData = new PrisonerData(def, info.dailyAIType);
            Transform spawnTr = anchor.prisonerSpawn != null ? anchor.prisonerSpawn : anchor.transform;
            GameObject pObj = Instantiate(prisonerPrefab, spawnTr.position, spawnTr.rotation);
            PrisonerController controller = pObj.GetComponent<PrisonerController>();

            controller.Initialize(newData, anchor, info.isSuspicious);
            _activePrisoners.Add(controller);
            anchor.IsOccupied = true;
        }
    }

    public void DespawnAllPrisoners()
    {
        foreach (var p in _activePrisoners)
        {
            if (p != null) Destroy(p.gameObject);
        }
        _activePrisoners.Clear();

        foreach (var anchor in _anchorMap.Values)
            anchor.IsOccupied = false;
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