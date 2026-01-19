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
    // ★ [핵심 수정] 이상현상 스폰 및 중복/타입 필터링 로직
    // =========================================================================
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anomalyDatabase == null) return;

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();

        // [중복 방지] 이미 스폰 처리된 Replacement Target(Floor, Wall 등) 기록
        HashSet<AnomalyTargetType> processedReplacements = new HashSet<AnomalyTargetType>();

        // 1. 방 주인 타입 확인
        PrisonerType residentType = GetPrisonerType(cellId);

        // 2. 오늘의 범인 선정
        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious && dailyList.Count > 0)
        {
            int rndIndex = UnityEngine.Random.Range(0, dailyList.Count);
            culpritDef = dailyList[rndIndex];
        }

        // 3. [정렬] 범인(100) > 개별(50) > 상시 노출(10) 순으로 처리하여 중요 아이템 선점 보장
        var sortedDefs = anomalyDatabase.defs.OrderByDescending(def => {
            if (def == culpritDef) return 100;
            if (def.category == AnomalyCategory.Individual && def.targetPrisoner == residentType) return 50;
            if (def.alwaysSpawnNormal) return 10;
            return 0;
        }).ToList();

        // 4. 순회 및 스폰
        foreach (var def in sortedDefs)
        {
            bool isCulprit = (def == culpritDef);
            bool shouldSpawn = false;

            // --- [핵심] 스폰 여부 판별 ---
            if (isCulprit)
            {
                shouldSpawn = true; // 범인은 무조건 스폰
            }
            else
            {
                // 범인이 아닌 경우 (Normal 버전 스폰 체크)
                if (def.alwaysSpawnNormal)
                {
                    // ★ 여기가 수정된 포인트입니다.
                    // AlwaysNormal이 켜져 있더라도, 'Individual' 카테고리라면 '방 주인'과 타입이 맞아야만 합니다.
                    if (def.category == AnomalyCategory.Individual)
                    {
                        if (def.targetPrisoner == residentType) shouldSpawn = true;
                    }
                    else
                    {
                        // Common(공통) 아이템이면 죄수 타입 상관없이 스폰
                        shouldSpawn = true;
                    }
                }
                else if (def.category == AnomalyCategory.Individual && def.targetPrisoner == residentType)
                {
                    // Always 체크가 없더라도 Individual 조건이 맞으면 스폰 (옵션)
                    shouldSpawn = true;
                }
            }

            if (!shouldSpawn) continue;

            // [중복 방지] 이미 처리된 Replacement 위치라면 건너뜀 (Floor_A가 나왔는데 Floor_N이 또 나오는 것 방지)
            if (def.targetType != AnomalyTargetType.Slot && processedReplacements.Contains(def.targetType))
            {
                continue;
            }

            // 프리팹 결정
            GameObject prefabToSpawn = isCulprit ? def.suspiciousPrefab : def.normalPrefab;
            if (prefabToSpawn == null) continue;

            // --- 실제 스폰 실행 ---
            if (def.targetType != AnomalyTargetType.Slot)
            {
                // [Replacement 타입] (Floor, Toilet 등)
                if (anchor.structure != null)
                {
                    GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                    if (defaultObj != null)
                    {
                        // 기존 오브젝트 끄고 새 것 생성
                        defaultObj.SetActive(false);

                        var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);
                        var actor = go.GetComponent<AnomalyActor>();
                        if (actor != null) actor.Init(cellId, def, isCulprit);
                        content.anomalies.Add(go);

                        // 처리 완료 마킹
                        processedReplacements.Add(def.targetType);
                    }
                    else
                    {
                        if (verboseLog) Debug.LogWarning($"[Spawn] {cellId} ({residentType}) - {def.targetType} 교체 실패: CellStructure에 연결된 기본 오브젝트가 없습니다.");
                    }
                }
            }
            else
            {
                // [Slot 타입] (Poster, Props)
                // 해당 Kind에 맞는 슬롯 찾기
                var candidateSlots = availableSlots.Where(s => s.kind == def.kind).ToList();

                if (candidateSlots.Count > 0)
                {
                    var targetSlot = candidateSlots[UnityEngine.Random.Range(0, candidateSlots.Count)];
                    availableSlots.Remove(targetSlot); // 슬롯 소비

                    var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                    var actor = go.GetComponent<AnomalyActor>();
                    if (actor != null) actor.Init(cellId, def, isCulprit);
                    content.anomalies.Add(go);
                }
                else
                {
                    // 우선순위 정렬 덕분에 Individual 아이템은 여기서 슬롯 부족이 뜰 확률이 낮습니다.
                    if (def.category == AnomalyCategory.Individual && verboseLog)
                    {
                        Debug.LogWarning($"[Spawn] {cellId} ({residentType}) 전용 아이템 '{def.name}' 스폰 실패: '{def.kind}' 슬롯 부족!");
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