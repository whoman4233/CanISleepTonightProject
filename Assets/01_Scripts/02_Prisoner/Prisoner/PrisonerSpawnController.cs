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
        if (contentRegistry != null) contentRegistry.ClearAll();

        if (anchorRegistry != null)
        {
            foreach (var anchor in anchorRegistry.GetAllAnchors())
            {
                if (anchor != null && anchor.structure != null)
                {
                    anchor.structure.ResetAllDefaults();
                    anchor.IsOccupied = false;
                }
            }
        }

        if (verboseLog) Debug.Log("[Spawn] Cleared all contents & Reset defaults for new day.");
    }

    public void SpawnForCell(string cellId, bool isSuspicious)
    {
        if (!ValidateRefs()) return;
        if (contentRegistry.TryGet(cellId, out _)) return;

        if (!anchorRegistry.TryGet(cellId, out var anchor) || anchor == null)
        {
            Debug.LogWarning($"[Spawn] Anchor missing for cell={cellId}");
            return;
        }

        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        if (existingData == null)
        {
            if (verboseLog) Debug.LogWarning($"[Spawn] No prisoner active for {cellId} today.");
            return;
        }

        // 3. 컨텐츠 등록
        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // 4. 죄수 생성
        PrisonerController controller = InstantiatePrisoner(anchor, existingData, isSuspicious);
        content.prisoner = controller;

        // 5. 프롭 생성
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
        }

        // 🔥 6. 이상현상 배정 및 생성 (죄수 타입 전달)
        AssignRandomAnomalies(anchor, existingData.definition.traitType);
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    // -----------------------------------------------------------------------
    // 내부 로직
    // -----------------------------------------------------------------------

    private void AssignRandomAnomalies(CellAnchor anchor, PrisonerType prisonerType)
    {
        if (anchor.currentDailyAnomalies == null)
            anchor.currentDailyAnomalies = new List<AnomalyDefinitionSO>();

        anchor.currentDailyAnomalies.Clear();

        if (anomalyDatabase == null || anomalyDatabase.defs == null) return;
        if (anchor.anomalySlots == null) return;

        foreach (var slot in anchor.anomalySlots)
        {
            // 🔥 [핵심 수정] 작성하신 SO 구조에 맞춰 필터링
            var possibleAnomalies = anomalyDatabase.defs
                .Where(a => a.kind == slot.kind) // 1. 슬롯 타입 일치 (침대 자리에 침대)
                .Where(a =>
                    // 2. 카테고리 및 죄수 타입 체크
                    a.category == AnomalyCategory.Common || // 공통이면 무조건 OK
                    (a.category == AnomalyCategory.Individual && a.targetPrisoner == prisonerType) // 개별이면 죄수 타입 일치해야 함
                )
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
        if (anchor.currentDailyAnomalies == null || anchor.currentDailyAnomalies.Count == 0) return;

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);

        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious)
        {
            int rndIndex = UnityEngine.Random.Range(0, anchor.currentDailyAnomalies.Count);
            culpritDef = anchor.currentDailyAnomalies[rndIndex];
            if (verboseLog) Debug.Log($"[Spawn] Cell {cellId} Culprit is {culpritDef.anomalyId}");
        }

        foreach (var def in anchor.currentDailyAnomalies)
        {
            bool isRealAnomaly = (def == culpritDef);

            // 🔥 [수정] 생성할 프리팹 결정 (진짜면 Suspicious, 아니면 Normal)
            // NormalPrefab이 없으면 null이 들어갑니다.
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            // CASE 1: 교체형 (TargetType != Slot) - 침대, 바닥, 벽 등
            if (def.targetType != AnomalyTargetType.Slot)
            {
                // 조건: 
                // 1. 진짜 이상현상이거나 (Floor_A)
                // 2. 가짜인데 '항상 생성' 체크가 되어있을 때 (Floor_N으로 굳이 바꿔야 할 때)
                if (isRealAnomaly || def.alwaysSpawnNormal)
                {
                    if (anchor.structure != null && prefabToSpawn != null)
                    {
                        // 기존 가구(Floor)를 찾아서 끕니다.
                        GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                        if (defaultObj != null)
                        {
                            defaultObj.SetActive(false); // 기존 끄기

                            // 그 위치/회전/부모 그대로 새 놈(Floor_A or Floor_N) 생성
                            var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);

                            var actor = go.GetComponent<AnomalyActor>();
                            if (actor != null) actor.Init(cellId, def, isRealAnomaly);

                            content.anomalies.Add(go);
                        }
                    }
                }
                // else: 아무것도 안 하면 '기존 Scene 오브젝트(Floor)'가 그대로 보임 (성능 이득)
            }
            // CASE 2: 추가형 (TargetType == Slot) - 시계, 포스터 등
            else
            {
                int slotIndex = availableSlots.FindIndex(s => s.kind == def.kind);

                if (slotIndex != -1)
                {
                    var targetSlot = availableSlots[slotIndex];
                    availableSlots.RemoveAt(slotIndex);

                    // 조건: 진짜거나, 가짜여도 생성해야 하는 경우
                    if (isRealAnomaly || def.alwaysSpawnNormal)
                    {
                        if (prefabToSpawn != null)
                        {
                            var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);

                            var actor = go.GetComponent<AnomalyActor>();
                            if (actor != null) actor.Init(cellId, def, isRealAnomaly);

                            content.anomalies.Add(go);
                        }
                    }
                }
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
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null &&
               prisonerPrefab != null && scheduleManager != null && anomalyDatabase != null;
    }
}