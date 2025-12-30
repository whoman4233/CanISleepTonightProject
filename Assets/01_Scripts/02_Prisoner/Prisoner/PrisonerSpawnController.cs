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
    [SerializeField] private GameObject cellPropPrefab;

    // [삭제됨] 임시 템플릿 ID (이제 스케줄 매니저가 관리함)
    // [SerializeField] private string defaultGoodTemplateId = "P_01";
    // [SerializeField] private string defaultBadTemplateId = "P_02";

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

            // 1. 이상현상 데이터 준비
            anomalyDistributor.DistributeAnomaliesForDay(currentDay);

            // 2. 오늘 활성화될 방 목록(Schedule) 가져오기
            var todaysPlan = scheduleManager.GetScheduleForDay(currentDay);

            // 3. 기존 오브젝트 정리 및 재생성
            ClearAllForNewDay();

            foreach (var kvp in todaysPlan)
            {
                string cellId = kvp.Key;
                bool isSuspicious = kvp.Value; // 오늘의 수상함 여부 (스케줄)

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

        // [변경 핵심 1] "임시 템플릿" 대신 "스케줄 매니저의 배정 정보" 사용
        // (이번 주 내내 이 방엔 누가 살고, 성향은 무엇인지 가져옴)
        var assignment = scheduleManager.GetAssignment(cellId);

        if (assignment == null)
        {
            if (verboseLog) Debug.LogWarning($"[Spawn] No prisoner assigned for {cellId} in Roster.");
            return;
        }

        string templateId = assignment.Value.templateId;
        PrisonerAIType assignedAIType = assignment.Value.aiType; // 랜덤 결정된 성향

        if (!prisonerDatabase.TryGet(templateId, out var def) || def == null) return;

        var content = new CellContentRegistry.CellContent();
        string instanceId = BuildInstanceId(cellId, def.templateId, 1);
        content.prisonerInstanceId = instanceId;

        // [변경 핵심 2] 성향(assignedAIType)을 함께 전달
        PrisonerController controller = InstantiatePrisoner(anchor, instanceId, def, assignedAIType, isSuspicious);
        content.prisoner = controller;

        // 2. 프롭 생성
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
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

    // [변경 핵심 3] 인자에 aiType 추가 및 데이터 생성 시 주입
    private PrisonerController InstantiatePrisoner(CellAnchor anchor, string instanceId, PrisonerDefinition def, PrisonerAIType aiType, bool isSuspicious)
    {
        var pGo = Instantiate(prisonerPrefab, anchor.prisonerSpawn.position, anchor.prisonerSpawn.rotation);
        pGo.name = $"Prisoner_{instanceId}";

        var controller = pGo.GetComponent<PrisonerController>();
        if (controller == null) controller = pGo.AddComponent<PrisonerController>();

        // [신규] PrisonerData 생성 시 '랜덤 결정된 성향(aiType)' 주입
        PrisonerData newData = new PrisonerData(def, aiType);

        controller.Initialize(newData, anchor, isSuspicious);

        return controller;
    }

    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anchor.currentDailyAnomalies == null || anchor.currentDailyAnomalies.Count == 0) return;

        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious)
        {
            int rndIndex = UnityEngine.Random.Range(0, anchor.currentDailyAnomalies.Count);
            culpritDef = anchor.currentDailyAnomalies[rndIndex];
        }

        foreach (var def in anchor.currentDailyAnomalies)
        {
            var validSlots = anchor.anomalySlots.FindAll(slot => slot.kind == def.kind);

            if (validSlots.Count == 0)
            {
                if (verboseLog) Debug.LogWarning($"[Spawn] Cell {cellId} has anomaly {def.anomalyId} assigned but no slot of kind {def.kind}");
                continue;
            }

            var targetSlot = validSlots[UnityEngine.Random.Range(0, validSlots.Count)];
            bool isRealAnomaly = isSuspicious && (def == culpritDef);
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            if (prefabToSpawn != null)
            {
                var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                go.name = isRealAnomaly ? $"Anomaly_{def.anomalyId}_SUS" : $"Prop_{def.anomalyId}_Normal";

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