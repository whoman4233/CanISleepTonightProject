using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject prisonerPrefab; // 죄수 프리팹
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase; // 데이터베이스 (랜덤 픽용)

    // 앵커는 씬에 배치된 물리적 위치들 (Inspector에서 할당)
    [SerializeField] private List<CellAnchor> cellAnchors;

    [Header("Grid Config")]
    [SerializeField] private int floors = 2;
    [SerializeField] private int cellsPerFloor = 8;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    // -------------------------------------------------------------
    // 내부 데이터 관리
    // -------------------------------------------------------------
    // 논리적 셀 데이터 (상태 관리용)
    private readonly Dictionary<string, CellRuntime> _runtimeCells = new();

    // 물리적 앵커 캐싱 (ID로 빠르게 찾기 위함)
    private Dictionary<string, CellAnchor> _anchorMap = new();

    // 현재 활성화된 죄수 컨트롤러 목록 (정산/관리용)
    private List<PrisonerController> _activePrisoners = new List<PrisonerController>();

    public IReadOnlyList<PrisonerController> ActivePrisoners => _activePrisoners;

    // 이벤트
    public event Action<string, bool> OnNoiseChanged; // (cellId, isNoisy)

    // 층별 활성 카운트
    public int ActiveCell1f { get; private set; }
    public int ActiveCell2f { get; private set; }

    private void Awake()
    {
        // 1. 물리 앵커 맵핑
        foreach (var anchor in cellAnchors)
        {
            if (anchor != null)
                _anchorMap[anchor.cellId] = anchor;
        }

        // 2. 논리 셀 데이터 구축
        BuildRuntimeCells();
    }

    // -------------------------------------------------------------
    // 1. 초기화 및 그리드 구축 (Logic)
    // -------------------------------------------------------------
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

                    IsActiveToday = true,
                    State = CellState.ActiveNoisy
                };
                _runtimeCells[id] = cell;
            }
        }
    }

    public static string MakeCellId(int floor, int number) => $"C_{floor}F_{number:00}";

    public CellRuntime GetCellRuntime(string cellId)
    {
        return _runtimeCells.TryGetValue(cellId, out var cell) ? cell : null;
    }

    public CellAnchor GetCellAnchor(string cellId)
    {
        return _anchorMap.TryGetValue(cellId, out var anchor) ? anchor : null;
    }

    // -------------------------------------------------------------
    // 2. 하루 스케줄 적용 및 죄수 스폰 (Core Logic)
    // -------------------------------------------------------------
    public void ApplyDailySchedule(Dictionary<string, DailyCellAssignment> assignments)
    {
        DespawnAllPrisoners();
        ResetCellsForNewDay();
        ClearAllAnomalies();

        ActiveCell1f = 0;
        ActiveCell2f = 0;

        foreach (var kvp in assignments)
        {
            string cellId = kvp.Key;
            DailyCellAssignment info = kvp.Value; // 구조체 가져오기

            var cellRuntime = GetCellRuntime(cellId);
            var cellAnchor = GetCellAnchor(cellId);

            if (cellRuntime == null || cellAnchor == null) continue;

            // 1. 방 상태 설정
            cellRuntime.IsActiveToday = true;
            cellRuntime.IsSuspicious = info.isSuspicious; // 수상함 여부
            SetNoisy(cellRuntime, true);
            cellRuntime.State = CellState.ActiveNoisy;

            if (cellRuntime.Floor == 1) ActiveCell1f++;
            else if (cellRuntime.Floor == 2) ActiveCell2f++;

            // 2. 죄수 스폰 (오늘의 성향 전달)
            SpawnPrisonerInCell(cellAnchor, info);

            // 3. 이상현상 스폰
            SpawnAnomaliesInCell(cellId, cellAnchor, info.isSuspicious);
        }

        if (verboseLog) Debug.Log($"[PrisonManager] Day Started. Active: {assignments.Count}");
    }

    // [신규] 로직 3번 구현: 이상현상 오브젝트 실제 생성
    private void SpawnAnomaliesInCell(string cellId, CellAnchor anchor, bool isSuspicious)
    {
        // 오늘 배정된 리스트가 없으면 할 일 없음
        if (anchor.currentDailyAnomalies == null || anchor.currentDailyAnomalies.Count == 0) return;

        // [핵심 로직 2번 해결]
        // 1. 범인(진짜 이상현상) 지목
        // 방이 수상하다면(isSuspicious == true) -> 리스트 중 1개를 랜덤으로 골라 범인으로 지정.
        // 방이 안 수상하다면 -> 범인은 없음 (null).
        AnomalyDefinitionSO culpritDef = null;

        if (isSuspicious)
        {
            int rndIndex = UnityEngine.Random.Range(0, anchor.currentDailyAnomalies.Count);
            culpritDef = anchor.currentDailyAnomalies[rndIndex];
        }

        // 2. 리스트에 있는 모든 요소를 생성 (반복문)
        foreach (var def in anchor.currentDailyAnomalies)
        {
            // 들어갈 슬롯 찾기
            var validSlots = anchor.anomalySlots.FindAll(slot => slot.kind == def.kind);
            if (validSlots.Count == 0) continue;

            // 슬롯 랜덤 선택
            var targetSlot = validSlots[UnityEngine.Random.Range(0, validSlots.Count)];

            // [중요 판별 로직]
            // "내가 지금 만드는 이 녀석(def)이 아까 지목한 범인(culpritDef)인가?"
            // 참이면 -> 진짜 이상현상 프리팹 사용
            // 거짓이면 -> (범인이 아니거나, 방 자체가 안 수상함) -> 정상 프리팹 사용
            bool isRealAnomaly = (def == culpritDef);

            // 프리팹 결정
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            if (prefabToSpawn != null)
            {
                var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);

                // 초기화
                var actor = go.GetComponent<AnomalyActor>();
                if (actor != null) actor.Init(cellId, def, isRealAnomaly);
            }
        }
    }

    // [신규] 하루 시작 시 기존 이상현상 삭제
    private void ClearAllAnomalies()
    {
        // 모든 앵커를 돌면서 생성된 AnomalyActor들을 찾아 삭제
        foreach (var anchor in _anchorMap.Values)
        {
            // AnomalyActor가 붙은 모든 자식 파괴
            var anomalies = anchor.GetComponentsInChildren<AnomalyActor>();
            foreach (var a in anomalies)
            {
                Destroy(a.gameObject);
            }
        }
    }

    // 실제 생성 로직 (Instantiate)
    private void SpawnPrisonerInCell(CellAnchor anchor, DailyCellAssignment info)
    {
        // 1. 스케줄 매니저에게 "이 방 주인(Template) 누구야?" 물어보기
        var scheduleMgr = FindObjectOfType<PrisonerScheduleManager>();
        var def = scheduleMgr.GetAssignedPrisonerDef(anchor.cellId); // 이제 Def만 가져옴

        if (def != null)
        {
            // 2. [핵심] 오늘 결정된 성향(info.dailyAIType)을 주입!
            PrisonerData newData = new PrisonerData(def, info.dailyAIType);

            Transform spawnTr = anchor.prisonerSpawn != null ? anchor.prisonerSpawn : anchor.transform;
            GameObject pObj = Instantiate(prisonerPrefab, spawnTr.position, spawnTr.rotation);
            PrisonerController controller = pObj.GetComponent<PrisonerController>();

            // 3. 초기화 (수상함 여부도 같이 전달)
            controller.Initialize(newData, anchor, info.isSuspicious);

            _activePrisoners.Add(controller);
            anchor.IsOccupied = true;
        }
        else
        {
            Debug.LogError($"[PrisonManager] No roster found for cell {anchor.cellId}");
        }
    }

    public void DespawnAllPrisoners()
    {
        foreach (var p in _activePrisoners)
        {
            if (p != null) Destroy(p.gameObject);
        }
        _activePrisoners.Clear();

        // 앵커 점유 상태 해제
        foreach (var anchor in _anchorMap.Values)
        {
            anchor.IsOccupied = false;
        }
    }

    private void ResetCellsForNewDay()
    {
        foreach (var c in _runtimeCells.Values)
            c.ResetForNewDay();
    }


    // -------------------------------------------------------------
    // 3. 상태 관리 및 처리 로직 (Resolving)
    // -------------------------------------------------------------
    public void ResolveAndDeactivateCell(string cellId)
    {
        var cell = GetCellRuntime(cellId);
        if (cell == null) return;

        cell.IsActiveToday = false;
        SetNoisy(cell, false);
        cell.State = CellState.Inactive;

        // 주의: 죄수 오브젝트를 여기서 바로 파괴할지, 하루 끝날 때 파괴할지는 기획에 따라 결정.
        // 보통은 '처리됨(Locked)' 상태로 두고 밤에 한꺼번에 치우는 것이 일반적입니다.
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

        if (verboseLog) Debug.Log($"[PrisonManager] Cell {cellId} resolved. Locked.");
    }

    // 소음 상태 변경
    private void SetNoisy(CellRuntime cell, bool noisy)
    {
        if (cell.IsNoisy == noisy) return;
        cell.IsNoisy = noisy;
        OnNoiseChanged?.Invoke(cell.CellId, noisy);
    }

    public CellRuntime GetCell(string cellId)
    {
        return GetCellRuntime(cellId);
    }

    // -------------------------------------------------------------
    // 4. Inspection Flow Support (State Machine에서 호출)
    // -------------------------------------------------------------

    /// <summary>
    /// [필수] 초기화가 안 되어 있다면 강제로 빌드 (안전장치)
    /// InspectionStateMachine의 Awake 등에서 호출됨
    /// </summary>
    public void BuildCellsIfNeeded()
    {
        if (_runtimeCells.Count > 0) return; // 이미 빌드됨
        BuildRuntimeCells();
    }

    /// <summary>
    /// [필수] 시간 초과 등으로 인해 점검을 강제로 중단하고 '퇴장' 처리만 할 때 사용
    /// (완료 처리가 아니라, 단순히 Inspecting 상태만 해제하고 다시 Active 상태로 되돌림)
    /// </summary>
    public void ForceReleaseInspectingOnly(string cellId)
    {
        var cell = GetCellRuntime(cellId);
        if (cell == null) return;

        // 1. 점검/진압 플래그 해제
        cell.IsInspectingNow = false;
        cell.IsSuppressing = false;
        cell.SuppressSuccess = false; // 진압 성공 여부도 초기화

        // 2. 상태 복구
        // 오늘 활성 방이었다면 다시 ActiveNoisy로 (재진입 가능하게)
        // 비활성 방이었다면 Inactive로
        if (cell.IsActiveToday)
            cell.State = CellState.ActiveNoisy;
        else
            cell.State = CellState.Inactive;

        if (verboseLog) Debug.Log($"[PrisonManager] Force released cell {cellId} (Timeout)");
    }

    public List<string> GetActiveCellIds()
    {
        var list = new List<string>();
        foreach (var cell in _runtimeCells.Values)
        {
            if (cell.IsActiveToday)
                list.Add(cell.CellId);
        }
        return list;
    }
}