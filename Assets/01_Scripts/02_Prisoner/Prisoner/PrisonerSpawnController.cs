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
    // [Removed] AnomalyDatabaseSO는 이제 Distributor만 봅니다.

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
                    // anchor.ClearDailyAnomalies(); // 리스트 초기화는 Distributor가 수행함
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

        // 1. 컨텐츠 컨테이너 생성
        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // 2. 죄수 생성
        PrisonerController controller = InstantiatePrisoner(anchor, existingData, isSuspicious);
        content.prisoner = controller;

        // 3. 프롭 생성
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
        }

        // 🔥 4. 이상현상 소환 실행
        // (이미 Distributor가 anchor.currentDailyAnomalies를 채워뒀다고 가정)
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    // -----------------------------------------------------------------------
    // 내부 로직 (선택 로직은 제거되고 생성 로직만 남음)
    // -----------------------------------------------------------------------

    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        // 리스트가 비어있으면 할 일 없음
        if (anchor.currentDailyAnomalies == null || anchor.currentDailyAnomalies.Count == 0) return;

        // Slot 위치 관리를 위해 복사본 사용
        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);

        // 범인(Culprit) 선정 로직
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

            // 생성할 프리팹 결정 (진짜면 Suspicious, 아니면 Normal)
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            // --------------------------------------------------
            // CASE 1: 교체형 (가구 등)
            // --------------------------------------------------
            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (isRealAnomaly || def.alwaysSpawnNormal)
                {
                    if (anchor.structure != null && prefabToSpawn != null)
                    {
                        GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                        if (defaultObj != null)
                        {
                            defaultObj.SetActive(false); // 기존 가구 숨김

                            // 새 가구 생성 (부모는 기존 가구의 부모로 설정)
                            var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);

                            var actor = go.GetComponent<AnomalyActor>();
                            if (actor != null) actor.Init(cellId, def, isRealAnomaly);

                            content.anomalies.Add(go);
                        }
                    }
                }
            }
            // --------------------------------------------------
            // CASE 2: 추가형 (Slot - 포스터, 시계 등)
            // --------------------------------------------------
            else
            {
                // 해당 종류(Kind)에 맞는 빈 슬롯 찾기
                int slotIndex = availableSlots.FindIndex(s => s.kind == def.kind);

                if (slotIndex != -1)
                {
                    var targetSlot = availableSlots[slotIndex];
                    availableSlots.RemoveAt(slotIndex); // 슬롯 사용 처리

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
               prisonerPrefab != null && scheduleManager != null;
    }
}