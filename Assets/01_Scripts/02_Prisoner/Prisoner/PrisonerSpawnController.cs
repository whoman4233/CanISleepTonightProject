using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    [SerializeField] private GameObject cellPropPrefab; // [추가] 프롭 프리팹

    [Header("Template Pick (임시)")]
    [SerializeField] private string defaultGoodTemplateId = "P_01";
    [SerializeField] private string defaultBadTemplateId = "P_02";

    [Header("Anomaly Data")]
    [SerializeField] private AnomalyDatabaseSO anomalyDatabase;

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
            int currentDay = GameManager.Instance.CurrentDay; // 현재 날짜 가져오기

            // 1. [기획 2번] 오늘 각 방에서 일어날 수 있는 이상현상 목록 배포
            anomalyDistributor.DistributeAnomaliesForDay(currentDay);

            // 2. [기획 1번] 미리 짜둔 스케줄표(Assignment) 가져오기
            var todaysPlan = scheduleManager.GetScheduleForDay(currentDay);

            // 3. 스케줄대로 생성 (Dictionary 키(CellId)가 활성 방 목록임)
            ClearAllForNewDay();

            foreach (var kvp in todaysPlan)
            {
                string cellId = kvp.Key;
                bool isSuspicious = kvp.Value; // 미리 정해진 수상함 여부

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

    public void SpawnForToday(List<string> activeCellIds, Func<string, bool> isSuspiciousByCell)
    {
        if (!ValidateRefs()) return;

        foreach (var cellId in activeCellIds)
        {
            if (string.IsNullOrWhiteSpace(cellId)) continue;
            bool isSuspicious = isSuspiciousByCell != null && isSuspiciousByCell(cellId);
            SpawnForCell(cellId, isSuspicious);
        }
    }

    public void SpawnForCell(string cellId, bool isSuspicious)
    {
        if (!ValidateRefs()) return;
        if (contentRegistry.TryGet(cellId, out _)) return;

        if (!anchorRegistry.TryGet(cellId, out var anchor) || anchor == null || anchor.prisonerSpawn == null)
        {
            Debug.LogWarning($"[Spawn] Anchor missing for cell={cellId}");
            return;
        }

        string templateId = isSuspicious ? defaultBadTemplateId : defaultGoodTemplateId;
        if (!prisonerDatabase.TryGet(templateId, out var def) || def == null) return;

        var content = new CellContentRegistry.CellContent();
        string instanceId = BuildInstanceId(cellId, def.templateId, 1);
        content.prisonerInstanceId = instanceId;

        // 1. 죄수 생성
        var pGo = InstantiatePrisoner(anchor, instanceId, def, isSuspicious, out var actor);
        content.prisoner = actor;

        // 2. [추가] 프롭(Prop) 생성 로직
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo; // Registry에 등록하여 나중에 삭제 가능하게 함
        }
        else
        {
            // 디버그용 (필요 없다면 주석 처리)
            if (cellPropPrefab == null && verboseLog) Debug.LogWarning($"[Spawn] CellPropPrefab is null.");
            if (anchor.propSpawnPoint == null && verboseLog) Debug.LogWarning($"[Spawn] PropSpawnPoint is null in Anchor {cellId}");
        }

        // 3. 이상현상 요소 생성
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
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

    private GameObject InstantiatePrisoner(CellAnchor anchor, string instanceId, PrisonerDefinition def, bool isSuspicious, out PrisonerActor actor)
    {
        var pGo = Instantiate(prisonerPrefab, anchor.prisonerSpawn.position, anchor.prisonerSpawn.rotation);
        pGo.name = $"Prisoner_{instanceId}";

        actor = pGo.GetComponent<PrisonerActor>();
        if (actor == null) actor = pGo.AddComponent<PrisonerActor>();
        actor.Init(anchor.cellId, instanceId, def, isSuspicious);

        var fsm = pGo.GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            fsm.InspectionPoint = anchor.inspectionPoint;
        }

        return pGo;
    }

    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        // 오늘 배정된 리스트가 없으면 리턴
        if (anchor.currentDailyAnomalies == null || anchor.currentDailyAnomalies.Count == 0) return;

        // 이 방이 수상하다면, 배정된 리스트 중 '누가 범인(진짜 이상현상)'일지 하나 정함
        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious)
        {
            int rndIndex = UnityEngine.Random.Range(0, anchor.currentDailyAnomalies.Count);
            culpritDef = anchor.currentDailyAnomalies[rndIndex];
        }

        // [중요] 배정된 모든 이상현상 요소를 순회하며 생성 (없으면 Normal이라도 생성해야 함)
        foreach (var def in anchor.currentDailyAnomalies)
        {
            // 1. 이 데이터(def)가 들어갈 수 있는 슬롯 찾기
            // (예: kind가 BrickColor면 벽돌 슬롯을 찾아야 함)
            // 리스트에서 FindAll을 쓰면 여러 개일 경우 대응 가능
            var validSlots = anchor.anomalySlots.FindAll(slot => slot.kind == def.kind);

            if (validSlots.Count == 0)
            {
                Debug.LogWarning($"[Spawn] Cell {cellId} has anomaly {def.anomalyId} assigned but no slot of kind {def.kind}");
                continue;
            }

            // 2. 슬롯 중 하나 랜덤 선택 (보통 종류당 슬롯 하나겠지만)
            var targetSlot = validSlots[UnityEngine.Random.Range(0, validSlots.Count)];

            // 3. 진짜 이상현상인지 판별
            // (방이 수상함 AND 현재 루프 도는 데이터가 아까 정한 범인임)
            bool isRealAnomaly = isSuspicious && (def == culpritDef);

            // 4. 프리팹 결정
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            if (prefabToSpawn != null)
            {
                var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                go.name = isRealAnomaly ? $"Anomaly_{def.anomalyId}_SUS" : $"Prop_{def.anomalyId}_Normal";

                // Interaction이나 Actor 컴포넌트 초기화
                var actor = go.GetComponent<AnomalyActor>();
                if (actor != null) actor.Init(cellId, def, isRealAnomaly);

                content.anomalies.Add(go);
            }
        }
    }

    private bool ValidateRefs()
    {
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null && prisonerPrefab != null;
    }

    private static string BuildInstanceId(string cellId, string templateId, int index1Based)
        => $"{cellId}_{templateId}_{index1Based:00}";
}