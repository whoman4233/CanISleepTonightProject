using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PrisonerSpawnController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private CellContentRegistry contentRegistry;
    [SerializeField] private PrisonerScheduleManager scheduleManager;

    // 🔥 [수정] 리스트 직접 할당 -> 데이터베이스 SO 참조로 변경
    [SerializeField] private AnomalyDatabaseSO anomalyDatabase;

    [Header("Prisoner Prefab")]
    [SerializeField] private GameObject prisonerPrefab;

    [Header("Cell Prop")]
    [SerializeField] private GameObject cellPropPrefab;

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

    // -----------------------------------------------------------------------
    // [외부 호출용] PrisonManager가 호출
    // -----------------------------------------------------------------------

    public void ClearAllForNewDay()
    {
        if (contentRegistry == null) return;
        contentRegistry.ClearAll();
        if (verboseLog) Debug.Log("[Spawn] Cleared all cell contents for new day.");
    }

    public void SpawnForCell(string cellId, bool isSuspicious)
    {
        if (!ValidateRefs()) return;
        if (contentRegistry.TryGet(cellId, out _)) return;

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

        // 4. 죄수 스폰
        PrisonerController controller = InstantiatePrisoner(anchor, existingData, isSuspicious);
        content.prisoner = controller;

        // 5. 프롭 스폰
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
        }

        // 6-1. 이상현상 데이터 배정 (DB 사용)
        AssignRandomAnomalies(anchor);

        // 6-2. 이상현상 실제 생성
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    // -----------------------------------------------------------------------
    // 내부 로직
    // -----------------------------------------------------------------------

    private void AssignRandomAnomalies(CellAnchor anchor)
    {
        if (anchor.currentDailyAnomalies == null)
            anchor.currentDailyAnomalies = new List<AnomalyDefinitionSO>();

        anchor.currentDailyAnomalies.Clear();

        // 🔥 [수정] 데이터베이스(SO) 안의 리스트(.defs)를 참조합니다.
        if (anomalyDatabase == null || anomalyDatabase.defs == null || anomalyDatabase.defs.Count == 0) return;
        if (anchor.anomalySlots == null || anchor.anomalySlots.Count == 0) return;

        foreach (var slot in anchor.anomalySlots)
        {
            // DB에서 해당 슬롯 타입에 맞는 이상현상 필터링
            var possibleAnomalies = anomalyDatabase.defs
                .Where(a => a.kind == slot.kind)
                .ToList();

            if (possibleAnomalies.Count > 0)
            {
                var picked = possibleAnomalies[UnityEngine.Random.Range(0, possibleAnomalies.Count)];

                if (!anchor.currentDailyAnomalies.Contains(picked))
                {
                    anchor.currentDailyAnomalies.Add(picked);
                }
            }
        }
    }

    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        List<AnomalyDefinitionSO> assignedList = anchor.currentDailyAnomalies;
        if (assignedList == null || assignedList.Count == 0) return;

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);

        var spawnableDefinitions = assignedList
            .Where(def => availableSlots.Any(slot => slot.kind == def.kind))
            .ToList();

        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious && spawnableDefinitions.Count > 0)
        {
            int rndIndex = UnityEngine.Random.Range(0, spawnableDefinitions.Count);
            culpritDef = spawnableDefinitions[rndIndex];
            if (verboseLog) Debug.Log($"[Spawn] Cell {cellId} Culprit is {culpritDef.anomalyId}");
        }

        foreach (var def in spawnableDefinitions)
        {
            var targetSlotIndex = availableSlots.FindIndex(s => s.kind == def.kind);
            if (targetSlotIndex == -1) continue;

            var targetSlot = availableSlots[targetSlotIndex];
            availableSlots.RemoveAt(targetSlotIndex);

            bool isRealAnomaly = (def == culpritDef);
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            if (prefabToSpawn != null)
            {
                var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                go.name = isRealAnomaly ? $"[ANOMALY] {def.anomalyId}" : $"[Normal] {def.anomalyId}";

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
        if (fsm != null) fsm.ChangeState(fsm.CombatState);
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
        // anomalyDatabase도 필수 체크에 포함
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null &&
               prisonerPrefab != null && scheduleManager != null && anomalyDatabase != null;
    }
}