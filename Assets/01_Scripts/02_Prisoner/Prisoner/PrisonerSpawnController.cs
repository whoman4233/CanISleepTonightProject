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


        // ================================================================
        // PART 1. 슬롯형 (기존 로직 유지) - 빈 공간에 생성되는 것들
        // ================================================================
        if (anchor.anomalySlots != null)
        {
            foreach (var slot in anchor.anomalySlots)
            {
                var possibleAnomalies = anomalyDatabase.defs
                    .Where(a => a.kind == slot.kind)
                    .Where(a => a.targetType == AnomalyTargetType.Slot) // 명시적으로 Slot 타입만
                    .Where(a => CheckCategoryAndType(a, prisonerType))  // 조건 체크 함수로 분리 추천
                    .ToList();

                if (possibleAnomalies.Count > 0)
                {
                    AddUniqueAnomaly(anchor, possibleAnomalies);
                }
            }
        }

        // ================================================================
        // PART 2. [추가됨] 교체형 (Structure) - 침대, 변기 등 가구 교체
        // ================================================================
        if (anchor.structure != null)
        {
            // 1. DB에서 'Slot'이 아닌(교체형) 모든 이상현상을 가져옴
            var replacementAnomalies = anomalyDatabase.defs
     .Where(a => a.targetType != AnomalyTargetType.Slot)
     .Where(a => CheckCategoryAndType(a, prisonerType))
     .GroupBy(a => a.targetType); // 타겟 타입별로 묶음


            foreach (var group in replacementAnomalies)
            {
                // 해당 가구가 실제로 있는지 확인
                if (anchor.structure.GetDefaultObject(group.Key) != null)
                {
                    // 그 타입의 이상현상 중 하나만 랜덤으로 선정
                    var picked = group.ElementAt(UnityEngine.Random.Range(0, group.Count()));
                    anchor.currentDailyAnomalies.Add(picked);
                }
            }
        }
    }

    // [도우미 함수 1] 조건 체크 (코드 중복 방지)
    private bool CheckCategoryAndType(AnomalyDefinitionSO a, PrisonerType pType)
    {
        return a.category == AnomalyCategory.Common ||
               (a.category == AnomalyCategory.Individual && a.targetPrisoner == pType);
    }

    // [도우미 함수 2] 중복 없이 추가
    private void AddUniqueAnomaly(CellAnchor anchor, List<AnomalyDefinitionSO> candidates)
    {
        if (candidates.Count == 0) return;
        var picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        if (!anchor.currentDailyAnomalies.Contains(picked))
        {
            anchor.currentDailyAnomalies.Add(picked);
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

            // CASE 1: 교체형 (TargetType != Slot)
            if (def.targetType != AnomalyTargetType.Slot)
            {
                // 디버그 로그 추가
                if (verboseLog)
                {
                    Debug.Log($"[SpawnCheck] Type: {def.targetType}, Real: {isRealAnomaly}, " +
                              $"StructureExist: {anchor.structure != null}, Prefab: {prefabToSpawn}");
                }

                if (isRealAnomaly || def.alwaysSpawnNormal)
                {
                    if (anchor.structure != null && prefabToSpawn != null)
                    {
                        GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);

                        // 디버그 로그 추가
                        if (defaultObj == null) Debug.LogError($"[SpawnError] {def.targetType}에 해당하는 Default Object를 CellStructure에서 찾지 못했습니다.");

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