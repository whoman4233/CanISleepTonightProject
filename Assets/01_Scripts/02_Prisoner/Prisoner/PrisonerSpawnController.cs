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
        if (evt.Phase == GamePhase.Briefing)
        {
            if (verboseLog) Debug.Log("[Spawn] Briefing Phase Started. Spawning Prisoners...");

            ClearAllForNewDay();

            List<string> allCellIds = anchorRegistry.GetAllCellIds(); // AnchorRegistry에 이 메서드가 있다고 가정

            SpawnForToday(allCellIds, (cellId) => UnityEngine.Random.value > 0.5f);
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
        var pGo = InstantiatePrisoner(anchor, instanceId, def, out var actor);
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
        SpawnAllAnomaliesInSlots(cellId, anchor, isSuspicious, content);

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

    private GameObject InstantiatePrisoner(CellAnchor anchor, string instanceId, PrisonerDefinition def, out PrisonerActor actor)
    {
        var pGo = Instantiate(prisonerPrefab, anchor.prisonerSpawn.position, anchor.prisonerSpawn.rotation);
        pGo.name = $"Prisoner_{instanceId}";

        actor = pGo.GetComponent<PrisonerActor>();
        if (actor == null) actor = pGo.AddComponent<PrisonerActor>();
        actor.Init(anchor.cellId, instanceId, def);

        var fsm = pGo.GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            fsm.InspectionPoint = anchor.inspectionPoint;
        }

        return pGo;
    }

    private void SpawnAllAnomaliesInSlots(string cellId, CellAnchor anchor, bool roomIsSuspicious, CellContentRegistry.CellContent content)
    {
        if (anchor.anomalySlots == null || anchor.anomalySlots.Count == 0) return;
        if (anomalyDatabase == null || anomalyDatabase.defs == null) return;

        var slots = anchor.anomalySlots;
        int suspiciousSlotIndex = roomIsSuspicious ? UnityEngine.Random.Range(0, slots.Count) : -1;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            var matches = anomalyDatabase.defs.FindAll(d => d.kind == slot.kind);
            if (matches.Count == 0) continue;

            var def = matches[UnityEngine.Random.Range(0, matches.Count)];
            bool isThisOneSuspicious = (i == suspiciousSlotIndex);
            GameObject prefab = isThisOneSuspicious ? def.suspiciousPrefab : def.normalPrefab;
            if (prefab == null) continue;

            var go = Instantiate(prefab, slot.transform.position, slot.transform.rotation, slot.transform);
            go.name = $"Anomaly_{cellId}_{def.anomalyId}_{(isThisOneSuspicious ? "S" : "N")}";

            var actor = go.GetComponent<AnomalyActor>();
            if (actor == null) actor = go.AddComponent<AnomalyActor>();
            actor.Init(cellId, def, isThisOneSuspicious);

            content.anomalies.Add(go);
        }
    }

    private bool ValidateRefs()
    {
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null && prisonerPrefab != null;
    }

    private static string BuildInstanceId(string cellId, string templateId, int index1Based)
        => $"{cellId}_{templateId}_{index1Based:00}";
}