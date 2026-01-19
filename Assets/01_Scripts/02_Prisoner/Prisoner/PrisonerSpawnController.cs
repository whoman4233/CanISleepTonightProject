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
    // ★ [최종] Decorative 집중 디버깅 (왜 안 나오는지 사유 출력)
    // =========================================================================
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anomalyDatabase == null) return;

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        HashSet<AnomalyTargetType> processedReplacements = new HashSet<AnomalyTargetType>();
        PrisonerType residentType = GetPrisonerType(cellId);

        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();
        AnomalyDefinitionSO culpritDef = (isSuspicious && dailyList.Count > 0) ? dailyList[0] : null;

        foreach (var def in anomalyDatabase.defs)
        {
            if (def == null) continue;

            bool isCulprit = (def == culpritDef);
            GameObject prefabToSpawn = null;

            // 1. 범인 처리 (최우선)
            if (isCulprit)
            {
                prefabToSpawn = def.suspiciousPrefab;
            }
            // 2. AlwaysSpawnNormal 처리 (조용히 처리, 로그 X)
            else if (def.alwaysSpawnNormal)
            {
                bool typeMatch = true;
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner != residentType)
                    typeMatch = false;

                if (typeMatch) prefabToSpawn = def.normalPrefab;
            }
            // 3. IsDecorative 처리 (여기가 핵심!)
            else if (def.isDecorative)
            {
                // [검사 1] 죄수 타입 일치 여부
                bool typeMatch = true;
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner != residentType)
                {
                    // 실패: 타입 불일치 -> 로그 찍고 스킵
                    if (verboseLog) Debug.Log($"<color=grey>[Decorative 실패] {def.name} -> 죄수타입 불일치 (Item:{def.targetPrisoner} != Room:{residentType})</color>");
                    continue;
                }

                // [검사 2] 자리(Replacement) 선점 여부
                if (def.targetType != AnomalyTargetType.Slot && processedReplacements.Contains(def.targetType))
                {
                    // 실패: 이미 다른 아이템(범인/Always/앞선 장식품)이 자리를 먹음
                    if (verboseLog) Debug.LogWarning($"<color=orange>[Decorative 실패] {def.name} -> 자리 꽉 참 ({def.targetType}에 이미 배치됨)</color>");
                    continue;
                }

                // [검사 3] 슬롯(Slot) 여유 확인
                if (def.targetType == AnomalyTargetType.Slot)
                {
                    bool hasSlot = availableSlots.Any(s => s.kind == def.kind);
                    if (!hasSlot)
                    {
                        // 실패: 해당 종류의 슬롯이 동남
                        if (verboseLog) Debug.LogWarning($"<color=orange>[Decorative 실패] {def.name} -> 슬롯 부족 (Available {def.kind} Slot: 0)</color>");
                        continue;
                    }
                }

                // 모든 검사 통과 -> 생성 준비 완료
                prefabToSpawn = def.normalPrefab;

                // 생성 성공 로그 (하늘색)
                if (verboseLog) Debug.Log($"<color=cyan>[Decorative 성공!] {def.name} -> {cellId} ({residentType}) 소환 확정</color>");
            }

            // 생성할 게 없으면 다음 아이템으로
            if (prefabToSpawn == null) continue;

            // 중복 체크 (위에서 했지만 안전장치)
            if (def.targetType != AnomalyTargetType.Slot && processedReplacements.Contains(def.targetType))
                continue;

            // --- 실제 생성 (Instantiate) ---
            GameObject spawnedGO = null;

            if (def.targetType != AnomalyTargetType.Slot)
            {
                // [Replacement]
                if (anchor.structure != null)
                {
                    GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                    if (defaultObj != null)
                    {
                        defaultObj.SetActive(false);
                        spawnedGO = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);
                        processedReplacements.Add(def.targetType);
                    }
                    else
                    {
                        Debug.LogError($"[치명적 오류] {def.name} -> CellStructure에 '{def.targetType}' 연결 안 됨!");
                    }
                }
            }
            else
            {
                // [Slot]
                var candidateSlots = availableSlots.Where(s => s.kind == def.kind).ToList();
                if (candidateSlots.Count > 0)
                {
                    var targetSlot = candidateSlots[UnityEngine.Random.Range(0, candidateSlots.Count)];
                    availableSlots.Remove(targetSlot);
                    spawnedGO = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                }
            }

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