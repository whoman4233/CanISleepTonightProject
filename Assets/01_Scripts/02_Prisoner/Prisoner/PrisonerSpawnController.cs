using System;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerSpawnController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private CellContentRegistry contentRegistry;

    [Header("Spawn Prefabs (Fallback)")]
    [Tooltip("AnomalyDatabaseSO가 비어있을 때만 쓰는 임시 프리팹")]
    [SerializeField] private GameObject anomalyFallbackPrefab;

    [Header("Prisoner Prefab")]
    [SerializeField] private GameObject prisonerPrefab;

    [Header("Player (Bad AI target)")]
    [SerializeField] private Transform player;

    [Header("Template Pick (임시)")]
    [Tooltip("감방 배치 데이터 전이라 임시로 타입별 템플릿을 지정")]
    [SerializeField] private string defaultGoodTemplateId = "P_01";
    [SerializeField] private string defaultBadTemplateId = "P_02";

    [Header("Anomaly Data")]
    [SerializeField] private AnomalyDatabaseSO anomalyDatabase;
    [Min(0)]
    [SerializeField] private int suspiciousAnomalyCount = 1; // 수상 방에 몇 개 깔지(임시)
    [Min(0)]
    [SerializeField] private int normalAnomalyCount = 1;     // 정상 방에도 검사요소를 둘지(임시)

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

    /// <summary>
    /// 새로운 하루 시작 전에 기존 콘텐츠를 싹 비우고 싶으면 호출하세요.
    /// (DayStart/Standby 시작 타이밍에 연결)
    /// </summary>
    public void ClearAllForNewDay()
    {
        if (contentRegistry == null) return;
        contentRegistry.ClearAll();
        if (verboseLog) Debug.Log("[Spawn] Cleared all cell contents for new day.");
    }

    /// <summary>
    /// Standby 직후: 오늘 활성 방들에 죄수/이상현상 요소를 미리 생성
    /// </summary>
    public void SpawnForToday(List<string> activeCellIds, Func<string, bool> isSuspiciousByCell)
    {
        if (!ValidateRefs()) return;
        if (activeCellIds == null || activeCellIds.Count == 0)
        {
            Debug.LogWarning("[Spawn] activeCellIds is empty.");
            return;
        }

        foreach (var cellId in activeCellIds)
        {
            if (string.IsNullOrWhiteSpace(cellId)) continue;
            bool isSuspicious = isSuspiciousByCell != null && isSuspiciousByCell(cellId);
            SpawnForCell(cellId, isSuspicious);
        }
    }

    /// <summary>
    /// 특정 감방 1개에 콘텐츠 생성
    /// </summary>
    public void SpawnForCell(string cellId, bool isSuspicious)
    {
        if (!ValidateRefs()) return;
        if (string.IsNullOrWhiteSpace(cellId))
        {
            Debug.LogWarning("[Spawn] cellId is null/empty.");
            return;
        }

        // 이미 있으면 중복 생성 방지
        if (contentRegistry.TryGet(cellId, out _))
        {
            if (verboseLog) Debug.Log($"[Spawn] Skip (already spawned) cell={cellId}");
            return;
        }

        if (!anchorRegistry.TryGet(cellId, out var anchor) || anchor == null || anchor.prisonerSpawn == null)
        {
            Debug.LogWarning($"[Spawn] Anchor missing or prisonerSpawn not set for cell={cellId}");
            return;
        }

        // 수상/정상에 따라 임시 템플릿 선택(추후 cellId->templateId 테이블로 교체)
        string templateId = isSuspicious ? defaultBadTemplateId : defaultGoodTemplateId;

        if (!prisonerDatabase.TryGet(templateId, out var def) || def == null)
        {
            Debug.LogError($"[Spawn] Prisoner template not found: {templateId} (cell={cellId})");
            return;
        }

        // 콘텐츠 컨테이너
        var content = new CellContentRegistry.CellContent();

        // 1명 기본(추후 N명 확장 대비: InstanceId는 규칙만 잡아둠)
        string instanceId = BuildInstanceId(cellId, def.templateId, 1);
        content.prisonerInstanceId = instanceId;

        // 죄수 생성
        var pGo = InstantiatePrisoner(anchor, instanceId, def, out var actor);
        if (actor == null)
        {
            Debug.LogError($"[Spawn] Failed to create prisoner actor (cell={cellId})");
            return;
        }
        content.prisoner = actor;

        // 이상현상 요소 생성 (정상/수상 모두 가능)
        SpawnAnomalies(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);

        if (verboseLog)
        {
            Debug.Log($"[Spawn] cell={cellId} susp={isSuspicious} prisoner={instanceId} type={def.type} hp={def.hp} anomalies={content.anomalies.Count}");
        }
        else
        {
            Debug.Log($"[Spawn] cell={cellId} susp={isSuspicious} spawned={instanceId}");
        }
    }

    /// <summary>
    /// 진압 시작: 이미 생성된 죄수를 전투 모드로 전환
    /// </summary>
    private void HandleSuppressStart(string cellId)
    {
        if (contentRegistry == null)
        {
            Debug.LogError("[Combat] contentRegistry missing.");
            return;
        }

        if (!contentRegistry.TryGet(cellId, out var content) || content == null || content.prisoner == null)
        {
            Debug.LogError($"[Combat] No prisoner content for cell={cellId} (Standby spawn missing?)");
            return;
        }

        content.prisoner.SetCombatEnabled(true);

        if (verboseLog)
            Debug.Log($"[Combat] Enabled cell={cellId} prisoner={content.prisoner.InstanceId} type={content.prisoner.Type}");
        else
            Debug.Log($"[Combat] Enabled cell={cellId}");
    }

    /// <summary>
    /// 점검 완료(퇴장) 시 호출해서 해당 방 콘텐츠 정리
    /// </summary>
    public void DespawnCell(string cellId)
    {
        if (contentRegistry == null) return;
        contentRegistry.ClearCell(cellId);
        if (verboseLog) Debug.Log($"[Spawn] Despawn cell={cellId}");
    }

    // -------------------------
    // Internal helpers
    // -------------------------

    private bool ValidateRefs()
    {
        if (prisonerDatabase == null)
        {
            Debug.LogError("[Spawn] Missing ref: prisonerDatabase");
            return false;
        }
        if (anchorRegistry == null)
        {
            Debug.LogError("[Spawn] Missing ref: anchorRegistry");
            return false;
        }
        if (contentRegistry == null)
        {
            Debug.LogError("[Spawn] Missing ref: contentRegistry");
            return false;
        }
        if (prisonerPrefab == null)
        {
            Debug.LogError("[Spawn] Missing ref: prisonerPrefab");
            return false;
        }
        return true;
    }

    private static string BuildInstanceId(string cellId, string templateId, int index1Based)
        => $"{cellId}_{templateId}_{index1Based:00}";

    private GameObject InstantiatePrisoner(CellAnchor anchor, string instanceId, PrisonerDefinition def, out PrisonerActor actor)
    {
        actor = null;

        var pGo = Instantiate(prisonerPrefab, anchor.prisonerSpawn.position, anchor.prisonerSpawn.rotation);
        pGo.name = $"Prisoner_{instanceId}";

        actor = pGo.GetComponent<PrisonerActor>();
        if (actor == null) actor = pGo.AddComponent<PrisonerActor>();
        actor.Init(anchor.cellId, instanceId, def);

        // Bad AI는 전투 때만 켜짐(Actor.SetCombatEnabled에서 토글)
        if (def.type == PrisonerType.Bad)
        {
            var ai = pGo.GetComponent<PrisonerBadAI>();
            if (ai == null) ai = pGo.AddComponent<PrisonerBadAI>();
            if (player != null) ai.BindPlayer(player);
        }

        return pGo;
    }

    private void SpawnAnomalies(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        // root: 슬롯 없거나 fallback용
        var root = anchor.anomalyRoot != null ? anchor.anomalyRoot : anchor.transform;

        // 1) AnomalyDatabase 우선
        if (anomalyDatabase != null && anomalyDatabase.defs != null && anomalyDatabase.defs.Count > 0)
        {
            int count = isSuspicious
                ? Mathf.Clamp(suspiciousAnomalyCount, 0, anomalyDatabase.defs.Count)
                : Mathf.Clamp(normalAnomalyCount, 0, anomalyDatabase.defs.Count);

            SpawnFromDatabase(cellId, anchor, root, content, suspiciousVariant: isSuspicious, count: count);
            return;
        }

        // 2) Fallback: 수상 방이면 임시 프리팹 1개
        if (isSuspicious && anomalyFallbackPrefab != null)
        {
            var aGo = Instantiate(anomalyFallbackPrefab, root.position, root.rotation, root);
            aGo.name = $"Anomaly_{cellId}_Fallback";
            content.anomalies.Add(aGo);
        }
    }

    private void SpawnFromDatabase(
    string cellId,
    CellAnchor anchor,
    Transform fallbackRoot,
    CellContentRegistry.CellContent content,
    bool suspiciousVariant,
    int count)
    {
        if (count <= 0) return;

        var pool = new List<AnomalyDefinitionSO>(anomalyDatabase.defs);
        int spawnCount = Mathf.Min(count, pool.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            int idx = UnityEngine.Random.Range(0, pool.Count);
            var def = pool[idx];
            pool.RemoveAt(idx);

            GameObject prefab = suspiciousVariant ? def.suspiciousPrefab : def.normalPrefab;
            if (prefab == null) continue;

            //  슬롯 우선: kind에 맞는 슬롯 중 하나
            Transform parent = fallbackRoot;
            Vector3 pos = fallbackRoot.position;
            Quaternion rot = fallbackRoot.rotation;

            if (anchor.anomalySlots != null && anchor.anomalySlots.Count > 0)
            {
                var candidates = new List<AnomalySpawnSlot>();
                foreach (var s in anchor.anomalySlots)
                {
                    if (s == null) continue;
                    if (s.kind == def.kind) candidates.Add(s);
                }

                if (candidates.Count > 0)
                {
                    var slot = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    parent = slot.transform;
                    pos = slot.transform.position;
                    rot = slot.transform.rotation;
                }
            }

            var go = Instantiate(prefab, pos, rot, parent);
            go.name = $"Anomaly_{cellId}_{def.anomalyId}_{(suspiciousVariant ? "S" : "N")}";

            var actor = go.GetComponent<AnomalyActor>();
            if (actor == null) actor = go.AddComponent<AnomalyActor>();
            actor.Init(cellId, def, suspiciousVariant);

            content.anomalies.Add(go);
        }
    }

}
