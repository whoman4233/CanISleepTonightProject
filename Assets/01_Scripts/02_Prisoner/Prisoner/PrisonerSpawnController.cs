using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 리스트 필터링(Where, List 등)을 위해 추가

public class PrisonerSpawnController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private CellContentRegistry contentRegistry;
    [SerializeField] private PrisonerScheduleManager scheduleManager;
    [SerializeField] private AnomalyDistributor anomalyDistributor;

    [Header("Prisoner Prefab")]
    [SerializeField] private GameObject prisonerPrefab;

    [Header("Cell Prop")]
    [Tooltip("감방 내 책상 등에 배치될 기본 프롭 프리팹")]
    [SerializeField] private GameObject cellPropPrefab;

    [Header("Debug")]
    [SerializeField] private bool verboseLog;

    private Action<GamePhaseChangedEvent> _onGamePhaseChanged;

    private void Awake()
    {
        _onGamePhaseChanged = HandleGamePhaseChanged;
    }

    private void OnEnable()
    {
        PrisonerEventBus.OnSuppressSessionStarted += HandleSuppressStart;
        EventBus.Subscribe(_onGamePhaseChanged);
    }

    private void OnDisable()
    {
        PrisonerEventBus.OnSuppressSessionStarted -= HandleSuppressStart;
        EventBus.Unsubscribe(_onGamePhaseChanged);
    }

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.Phase == GamePhase.Briefing) // 하루 시작
        {
            int currentDay = GameManager.Instance.CurrentDay;

            // 1. 이상현상 데이터 분배 (전체적인 리스트 작성)
            anomalyDistributor.DistributeAnomaliesForDay(currentDay);

            // 2. 오늘 활성화될 방 목록 가져오기
            var todaysPlan = scheduleManager.GetScheduleForDay(currentDay);

            // 3. 초기화 및 스폰
            ClearAllForNewDay();

            foreach (var kvp in todaysPlan)
            {
                string cellId = kvp.Key;
                bool isSuspicious = kvp.Value;
                SpawnForCell(cellId, isSuspicious);
            }
        }
    }

    public void ClearAllForNewDay()
    {
        if (contentRegistry == null) return;
        contentRegistry.ClearAll();
        if (verboseLog) Debug.Log("[Spawn] Cleared all cell contents for new day.");
    }

    public void SpawnForCell(string cellId, bool isSuspicious)
    {
        if (!ValidateRefs()) return;
        if (contentRegistry.TryGet(cellId, out _)) return; // 이미 생성됨

        // 1. 앵커 확인
        if (!anchorRegistry.TryGet(cellId, out var anchor) || anchor == null)
        {
            Debug.LogWarning($"[Spawn] Anchor missing for cell={cellId}");
            return;
        }

        // 2. 죄수 데이터 확인
        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        if (existingData == null)
        {
            if (verboseLog) Debug.LogWarning($"[Spawn] No prisoner active for {cellId} today.");
            return;
        }

        // 3. 컨텐츠 등록 준비
        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // 4. 죄수 스폰 (데이터 주입)
        PrisonerController controller = InstantiatePrisoner(anchor, existingData, isSuspicious);
        content.prisoner = controller;

        // 5. 기본 프롭 스폰 (침대 옆 탁자 같은 고정 프롭)
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
        }

        // 6. [핵심] 이상현상(가구) 스폰 로직
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        // 레지스트리에 최종 등록
        contentRegistry.Set(cellId, content);
    }

    // ▼▼▼ [수정됨] 교집합 로직이 적용된 핵심 함수 ▼▼▼
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        // A. 데이터 리스트 (오늘 이 방에 배정된 이상현상들)
        List<AnomalyDefinitionSO> assignedList = anchor.currentDailyAnomalies;
        if (assignedList == null || assignedList.Count == 0) return;

        // B. 앵커 리스트 (이 방에 실제로 존재하는 슬롯들)
        // 슬롯을 복사해서 사용하는 이유는, 한 번 사용한 슬롯은 목록에서 빼기 위함입니다 (중복 배치 방지)
        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);

        // C. [교집합 필터링] 실제로 배치가 가능한 정의들만 골라냅니다.
        // "배정된 리스트 중에, 현재 방에 꽂을 슬롯이 하나라도 남아있는 놈들"
        var spawnableDefinitions = assignedList
            .Where(def => availableSlots.Any(slot => slot.kind == def.kind))
            .ToList();

        // D. 범인 선정 (이상현상 발생 예정이라면)
        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious && spawnableDefinitions.Count > 0)
        {
            // 주의: spawnableDefinitions(교집합) 안에서 뽑아야 "반드시" 스폰됩니다.
            int rndIndex = UnityEngine.Random.Range(0, spawnableDefinitions.Count);
            culpritDef = spawnableDefinitions[rndIndex];

            if (verboseLog) Debug.Log($"[Spawn] Cell {cellId} Culprit is {culpritDef.anomalyId}");
        }

        // E. 실제 스폰 루프
        foreach (var def in spawnableDefinitions)
        {
            // 이 정의(Definition)와 맞는 슬롯을 찾음
            var targetSlotIndex = availableSlots.FindIndex(s => s.kind == def.kind);

            // 위에서 필터링을 했으므로 이론상 무조건 존재하지만, 안전장치
            if (targetSlotIndex == -1) continue;

            var targetSlot = availableSlots[targetSlotIndex];

            // 사용한 슬롯은 리스트에서 제거 (한 자리에 두 개 겹침 방지)
            availableSlots.RemoveAt(targetSlotIndex);

            // F. 프리팹 결정 (범인이면 수상한 놈, 아니면 정상인 놈)
            bool isRealAnomaly = (def == culpritDef);
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            if (prefabToSpawn != null)
            {
                var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);

                // 이름 설정 (디버깅 용이)
                go.name = isRealAnomaly ? $"[ANOMALY] {def.anomalyId}" : $"[Normal] {def.anomalyId}";

                // Actor 초기화
                var actor = go.GetComponent<AnomalyActor>();
                if (actor != null) actor.Init(cellId, def, isRealAnomaly);

                content.anomalies.Add(go);
            }
        }
    }

    private void HandleSuppressStart(string cellId)
    {
        if (!contentRegistry.TryGet(cellId, out var content) || content == null || content.prisoner == null) return;

        var fsm = content.prisoner.GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            fsm.ChangeState(fsm.CombatState);
        }
    }

    private PrisonerController InstantiatePrisoner(CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (anchor.prisonerSpawn == null) return null;

        var pGo = Instantiate(prisonerPrefab, anchor.prisonerSpawn.position, anchor.prisonerSpawn.rotation);
        pGo.name = $"Prisoner_{data.ID}";

        var controller = pGo.GetComponent<PrisonerController>();
        if (controller == null) controller = pGo.AddComponent<PrisonerController>();

        controller.Initialize(data, anchor, isSuspicious);
        return controller;
    }

    private bool ValidateRefs()
    {
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null && prisonerPrefab != null;
    }
}