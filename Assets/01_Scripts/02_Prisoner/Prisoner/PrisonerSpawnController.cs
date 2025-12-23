using System;
using System.Collections.Generic;
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

    private void OnEnable()
    {
        PrisonerEventBus.OnSuppressSessionStarted += HandleSuppressStart;
    }

    private void OnDisable()
    {
        PrisonerEventBus.OnSuppressSessionStarted -= HandleSuppressStart;
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

        // 이제 FSM 상태만 변경해주면 AI가 알아서 동작합니다.
        var fsm = content.prisoner.GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            // 점검(Inspection) 중에 때리는 것과 별개로 '진압 모드' 버튼을 눌렀을 때의 처리
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

        // ✅ FSM 설정: 이동 위치 할당 (BadAI 로직은 삭제됨)
        var fsm = pGo.GetComponent<PrisonerFSM>();
        if (fsm != null)
        {
            fsm.InspectionPoint = anchor.inspectionPoint;
        }

        return pGo;
    }

    /// <summary>
    /// ✅ 핵심: 감방의 모든 슬롯을 채우고, 수상한 방이면 1개만 이상현상 적용
    /// </summary>
    private void SpawnAllAnomaliesInSlots(string cellId, CellAnchor anchor, bool roomIsSuspicious, CellContentRegistry.CellContent content)
    {
        if (anchor.anomalySlots == null || anchor.anomalySlots.Count == 0) return;
        if (anomalyDatabase == null || anomalyDatabase.defs == null) return;

        var slots = anchor.anomalySlots;

        // 1. 수상한 방이라면 어느 슬롯을 이상하게 만들지 미리 결정
        int suspiciousSlotIndex = roomIsSuspicious ? UnityEngine.Random.Range(0, slots.Count) : -1;

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;

            // 2. 해당 슬롯의 Kind(베개, 칫솔 등)와 일치하는 정의들 검색
            var matches = anomalyDatabase.defs.FindAll(d => d.kind == slot.kind);
            if (matches.Count == 0) continue;

            // 3. 매칭되는 것 중 하나 무작위 선택
            var def = matches[UnityEngine.Random.Range(0, matches.Count)];

            // 4. 결정된 suspiciousSlotIndex와 일치하면 수상한 프리팹 사용
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