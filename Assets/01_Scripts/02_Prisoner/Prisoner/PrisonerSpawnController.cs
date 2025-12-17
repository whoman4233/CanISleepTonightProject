using System.Collections.Generic;
using UnityEngine;

public class PrisonerSpawnController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private CellContentRegistry contentRegistry;

    [Header("Spawn Prefabs (MVP: 캡슐/큐브로 테스트 가능)")]
    [SerializeField] private GameObject prisonerPrefab;
    [SerializeField] private GameObject anomalyPrefab; // 수상 방이면 1개라도 세워두는 용도(임시)

    [Header("Player (Bad AI target)")]
    [SerializeField] private Transform player;

    [Header("Template Pick (임시)")]
    [Tooltip("감방 배치 데이터 전이라 임시로 타입별 템플릿을 지정")]
    [SerializeField] private string defaultGoodTemplateId = "P_01";
    [SerializeField] private string defaultBadTemplateId = "P_02";

    //private void OnEnable()
    //{
    //    PrisonerEventBus.OnSuppressSessionStarted += HandleSuppressStart;
    //}

    //private void OnDisable()
    //{
    //    PrisonerEventBus.OnSuppressSessionStarted -= HandleSuppressStart;
    //}

    /// <summary>
    /// Standby 직후: 오늘 활성 방들에 죄수/이상현상 요소를 미리 생성
    /// </summary>
    public void SpawnForToday(List<string> activeCellIds, System.Func<string, bool> isSuspiciousByCell)
    {
        if (prisonerDatabase == null || anchorRegistry == null || contentRegistry == null)
        {
            Debug.LogError("[Spawn] Missing refs (db/registry/contentRegistry)");
            return;
        }

        foreach (var cellId in activeCellIds)
        {
            bool isSuspicious = isSuspiciousByCell(cellId);
            SpawnForCell(cellId, isSuspicious);
        }
    }

    private void SpawnForCell(string cellId, bool isSuspicious)
    {
        // 이미 있으면 중복 생성 방지
        if (contentRegistry.TryGet(cellId, out _))
            return;

        if (!anchorRegistry.TryGet(cellId, out var anchor) || anchor.prisonerSpawn == null)
        {
            Debug.LogWarning($"[Spawn] Anchor missing for cell={cellId}");
            return;
        }

        // 수상/정상에 따라 임시 템플릿 선택(추후 cellId->templateId 테이블로 교체)
        string templateId = isSuspicious ? defaultBadTemplateId : defaultGoodTemplateId;

        if (!prisonerDatabase.TryGet(templateId, out var def))
        {
            Debug.LogError($"[Spawn] Template not found: {templateId}");
            return;
        }

        // 1명 기본(추후 N명 확장 가능)
        string instanceId = $"{cellId}_{def.templateId}_01";

        // 죄수 생성
        var pGo = Instantiate(prisonerPrefab, anchor.prisonerSpawn.position, anchor.prisonerSpawn.rotation);
        pGo.name = $"Prisoner_{instanceId}";

        var actor = pGo.GetComponent<PrisonerActor>();
        if (actor == null) actor = pGo.AddComponent<PrisonerActor>();
        actor.Init(instanceId, def);

        // Bad AI 붙이되, 전투 때만 켜짐
        if (def.type == PrisonerType.Bad)
        {
            var ai = pGo.GetComponent<PrisonerBadAI>();
            if (ai == null) ai = pGo.AddComponent<PrisonerBadAI>();
            if (player != null) ai.BindPlayer(player);
        }

        // 이상현상 요소(수상 방만)
        var content = new CellContentRegistry.CellContent
        {
            prisoner = actor,
            prisonerInstanceId = instanceId
        };

        //if (isSuspicious && anomalyPrefab != null)
        //{
        //    var root = anchor.anomalyRoot != null ? anchor.anomalyRoot : anchor.transform;
        //    var aGo = Instantiate(anomalyPrefab, root.position, root.rotation, root);
        //    aGo.name = $"Anomaly_{cellId}_01";
        //    content.anomalies.Add(aGo);
        //}

        contentRegistry.Set(cellId, content);

        Debug.Log($"[Spawn] cell={cellId} susp={isSuspicious} spawned={instanceId} type={def.type} hp={def.hp}");
    }

    /// <summary>
    /// 진압 시작: 이미 생성된 죄수를 전투 모드로 전환
    /// </summary>
    //private void HandleSuppressStart(string cellId)
    //{
    //    if (!contentRegistry.TryGet(cellId, out var content) || content.prisoner == null)
    //    {
    //        Debug.LogError($"[Spawn] No prisoner content for cell={cellId} (Did you spawn at Standby?)");
    //        return;
    //    }

    //    content.prisoner.SetCombatEnabled(true);
    //    Debug.Log($"[Combat] Enabled cell={cellId} prisoner={content.prisoner.InstanceId}");
    //}

    /// <summary>
    /// 점검 완료(퇴장) 시 호출해서 해당 방 콘텐츠 정리하고 싶으면 이 메서드 사용
    /// (현재 프로젝트에서 점검 완료 시점 훅만 연결하면 됨)
    /// </summary>
    public void DespawnCell(string cellId)
    {
        contentRegistry.ClearCell(cellId);
    }
}
