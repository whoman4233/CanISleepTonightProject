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

    [Header("Anomaly Database")]
    [SerializeField] private AnomalyDatabaseSO anomalyDatabase;

    [Header("Default Settings")]
    [SerializeField] private GameObject defaultPrisonerPrefab;
    [SerializeField] private GameObject cellPropPrefab;

    [Header("Special Spawn Settings")]
    [SerializeField] private Transform[] centerSpawnPoints;
    private int _currentCenterSpawnIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private void OnEnable() => PrisonerEventBus.OnSuppressSessionStarted += HandleSuppressStart;
    private void OnDisable() => PrisonerEventBus.OnSuppressSessionStarted -= HandleSuppressStart;

    public void ClearAllForNewDay()
    {
        _currentCenterSpawnIndex = 0;
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
    }

    public void SpawnForCell(string cellId, bool isSuspicious)
    {
        if (!ValidateRefs() || contentRegistry.TryGet(cellId, out _)) return;
        if (!anchorRegistry.TryGet(cellId, out var anchor)) return;

        // 1. 데이터 가져오기
        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        DailyRoleData dailyRole = scheduleManager.GetDailyRole(cellId);

        if (existingData == null) return;

        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // 2. 죄수 프리팹 결정
        GameObject prefabToUse = null;
        if (existingData.definition != null) prefabToUse = existingData.definition.prisonerPrefab;
        if (prefabToUse == null) prefabToUse = defaultPrisonerPrefab;

        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            string targetID = dailyRole.visualType.ToString();
            if (prisonerDatabase.TryGet(targetID, out PrisonerDefinition specialDef))
            {
                if (specialDef.prisonerPrefab != null) prefabToUse = specialDef.prisonerPrefab;
            }
        }

        // 3. 중앙 스폰 위치 결정
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;
        bool isCenterTarget = IsCenterSpawnTarget(dailyRole.visualType);

        if (isCenterTarget)
        {
            if (centerSpawnPoints != null && _currentCenterSpawnIndex < centerSpawnPoints.Length)
            {
                spawnPos = centerSpawnPoints[_currentCenterSpawnIndex].position;
                spawnRot = centerSpawnPoints[_currentCenterSpawnIndex].rotation;
                _currentCenterSpawnIndex++;
            }
        }

        // 4. 죄수 생성
        PrisonerController controller = InstantiatePrisoner(prefabToUse, spawnPos, spawnRot, anchor, existingData, isSuspicious);
        if (controller != null) content.prisoner = controller;

        // 5. 기본 프롭 생성
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            content.prop = propGo;
        }

        // 6. 이상현상 스폰 로직 실행 (수정됨)
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    private PrisonerController InstantiatePrisoner(GameObject prefab, Vector3 pos, Quaternion rot, CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (prefab == null) return null;
        var pGo = Instantiate(prefab, pos, rot);
        pGo.name = $"Prisoner_{data.ID}";

        int prisonerLayer = LayerMask.NameToLayer("Prisoner");
        if (prisonerLayer != -1) SetLayerRecursively(pGo, prisonerLayer);

        var controller = pGo.GetComponent<PrisonerController>();
        if (controller != null) controller.Initialize(data, anchor, isSuspicious);

        var dialogue = pGo.GetComponent<PrisonerDialogue>();
        if (dialogue != null)
        {
            var dailyRole = scheduleManager.GetDailyRole(anchor.cellId);
            dialogue.myVisualType = dailyRole.visualType;
        }
        return controller;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursively(child.gameObject, newLayer);
    }

    private bool IsCenterSpawnTarget(VisualAnomalyType type)
    {
        return type.ToString().StartsWith("Imposter") || type.ToString().StartsWith("Suspect");
    }

    private PrisonerType GetPrisonerType(string cellId)
    {
        var data = scheduleManager.GetPrisonerData(cellId);
        if (data != null && data.definition != null)
        {
            return data.definition.traitType;
        }
        return PrisonerType.None;
    }

    // =========================================================================
    // ★ [최종 수정] 장식용 플래그 + 죄수 타입 일치 로직
    // =========================================================================
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anomalyDatabase == null) return;

        // 1. 사용할 슬롯과 기존 배치된 구조물 리스트업
        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        HashSet<AnomalyTargetType> processedReplacements = new HashSet<AnomalyTargetType>();

        // 2. 현재 방 주인 타입 (Nervous, Muscular...)
        PrisonerType residentType = GetPrisonerType(cellId);

        // 3. 오늘의 범인(Culprit) 확인
        // (AnomalyDistributor가 anchor.currentDailyAnomalies에 넣어둔 것 중 하나)
        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();
        AnomalyDefinitionSO culpritDef = (isSuspicious && dailyList.Count > 0) ? dailyList[0] : null;

        // 4. 우선순위 정렬 (범인이 1순위 -> 그래야 자리를 먼저 차지함)
        var sortedDefs = anomalyDatabase.defs.OrderByDescending(def => {
            if (def == culpritDef) return 100; // 1순위: 범인
            return 0;
        }).ToList();

        // 5. 전체 DB 순회하며 배치 결정
        foreach (var def in sortedDefs)
        {
            if (def == null) continue;

            GameObject prefabToSpawn = null;
            bool isCulprit = (def == culpritDef);

            // [판단 로직 A] 이 아이템이 '오늘의 범인'인가?
            if (isCulprit)
            {
                prefabToSpawn = def.suspiciousPrefab; // 무조건 의심스러운 버전
            }
            // [판단 로직 B] 범인은 아니지만, Normal로 배치해야 하는가?
            else
            {
                // 조건 1: "장식용"이거나 "AlwaysSpawnNormal"이어야 함
                bool isProp = (def.isDecorative || def.alwaysSpawnNormal);

                if (isProp)
                {
                    // ★ 조건 2: [핵심] 죄수 타입이 맞아야 함!
                    if (def.category == AnomalyCategory.Individual)
                    {
                        // 개별 타입: 방 주인과 내 타겟이 일치해야만 생성
                        if (def.targetPrisoner == residentType)
                        {
                            prefabToSpawn = def.normalPrefab;
                        }
                    }
                    else
                    {
                        // 공통/특수 타입: 죄수 상관없이 생성
                        prefabToSpawn = def.normalPrefab;
                    }
                }
            }

            // 스폰 대상이 아니면(null) 패스
            if (prefabToSpawn == null) continue;

            // 이미 처리된 Replacement 위치(바닥, 벽 등)면 패스
            if (def.targetType != AnomalyTargetType.Slot && processedReplacements.Contains(def.targetType))
                continue;

            // --- 실제 생성 로직 (기존과 동일) ---
            GameObject spawnedGO = null;

            if (def.targetType != AnomalyTargetType.Slot)
            {
                // [Replacement] 바닥, 벽, 변기 등
                if (anchor.structure != null)
                {
                    GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                    if (defaultObj != null)
                    {
                        defaultObj.SetActive(false); // 기존 것 끄고
                        spawnedGO = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);

                        processedReplacements.Add(def.targetType); // 처리 완료 마킹
                    }
                    else
                    {
                        if (verboseLog) Debug.LogWarning($"[Spawn] {cellId} - {def.targetType} 교체 실패: CellStructure 미연결");
                    }
                }
            }
            else
            {
                // [Slot] 소품류
                var candidateSlots = availableSlots.Where(s => s.kind == def.kind).ToList();
                if (candidateSlots.Count > 0)
                {
                    var targetSlot = candidateSlots[UnityEngine.Random.Range(0, candidateSlots.Count)];
                    availableSlots.Remove(targetSlot); // 슬롯 소비

                    spawnedGO = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                }
                else
                {
                    // 슬롯 부족 로그 (Individual이 안 나오는 원인 파악용)
                    if (def.category == AnomalyCategory.Individual && verboseLog)
                    {
                        // Debug.LogWarning($"[Spawn] {cellId} ({residentType}) 슬롯 부족: {def.kind}");
                    }
                }
            }

            // Actor 초기화
            if (spawnedGO != null)
            {
                var actor = spawnedGO.GetComponent<AnomalyActor>();
                if (actor != null) actor.Init(cellId, def, isCulprit);
                content.anomalies.Add(spawnedGO);
            }
        }
    }

    private void HandleSuppressStart(string cellId)
    {
        if (!contentRegistry.TryGet(cellId, out var content) || content == null || content.prisoner == null) return;
        var fsm = content.prisoner.GetComponent<PrisonerFSM>();
        if (fsm != null) fsm.ChangeState(fsm.CombatState);
    }

    private bool ValidateRefs()
    {
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null && scheduleManager != null;
    }

    public void SpawnAllPrisoners()
    {
        if (anchorRegistry == null) return;
        var allCellIds = anchorRegistry.GetAllCellIds();
        foreach (var cellId in allCellIds)
        {
            var role = scheduleManager.GetDailyRole(cellId);
            SpawnForCell(cellId, role.isSuspicious);
        }
    }
}