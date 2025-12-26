using System;
using System.Collections.Generic;
using System.Linq; // List 변환을 위해 추가
using UnityEngine;

public class PrisonerSpawnController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private CellContentRegistry contentRegistry;

    [Header("Prisoner Prefab")]
    [SerializeField] private GameObject prisonerPrefab;

    [Header("Template Pick (임시)")]
    [SerializeField] private string defaultGoodTemplateId = "P_01";
    [SerializeField] private string defaultBadTemplateId = "P_02";

    [Header("Anomaly Data")]
    [SerializeField] private AnomalyDatabaseSO anomalyDatabase;

    [Header("Debug")]
    [SerializeField] private bool verboseLog;

    // [중요] EventBus의 WeakReference 이슈 방지를 위해 Action을 필드로 보관
    private Action<GamePhaseChangedEvent> _onGamePhaseChanged;

    private void Awake()
    {
        // 이벤트 핸들러 할당
        _onGamePhaseChanged = HandleGamePhaseChanged;
    }

    private void OnEnable()
    {
        // 기존 구독 유지
        PrisonerEventBus.OnSuppressSessionStarted += HandleSuppressStart;

        // [추가] GameManager의 페이즈 변경 이벤트 구독
        EventBus.Subscribe(_onGamePhaseChanged);
    }

    private void OnDisable()
    {
        PrisonerEventBus.OnSuppressSessionStarted -= HandleSuppressStart;

        // [추가] 구독 해지
        EventBus.Unsubscribe(_onGamePhaseChanged);
    }

    // [추가] 페이즈 변경 시 호출되는 콜백
    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.Phase == GamePhase.Briefing)
        {
            if (verboseLog) Debug.Log("[Spawn] Briefing Phase Started. Spawning Prisoners...");

            // 1. 기존 데이터 초기화
            ClearAllForNewDay();

            // 2. 스폰할 감방 ID 목록 가져오기 (AnchorRegistry에 등록된 모든 방)
            // CellAnchorRegistry에 모든 키를 가져오는 기능이 있다고 가정하거나, 
            // 없다면 아래처럼 anchorRegistry 내부 구현에 맞춰 가져와야 합니다.
            // 여기서는 예시로 anchorRegistry가 Dictionary 등을 가지고 있다고 가정하고 Keys를 리스트로 변환합니다.
            // 만약 anchorRegistry에 public 메서드로 ID 목록을 얻는 게 없다면 추가가 필요합니다.
            List<string> allCellIds = anchorRegistry.GetAllCellIds();

            // 3. 오늘의 죄수 생성 (수상함 여부 로직은 임시로 50% 확률 적용)
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

        // 2. 이상현상 요소 생성 (모든 슬롯 채우기 로직)
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