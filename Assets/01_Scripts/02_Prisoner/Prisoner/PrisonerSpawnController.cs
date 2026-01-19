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

    // [삭제됨] RiotSystem 참조 제거

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

        // 6. 이상현상 및 전용 소품 소환 (Riot 로직 제거됨)
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

    // ★ [핵심] 사용자의 PrisonerType 사용
    private PrisonerType GetPrisonerType(string cellId)
    {
        var data = scheduleManager.GetPrisonerData(cellId);
        // ★ PrisonerDefinition에 'prisonerType' 필드가 있어야 합니다.
        if (data != null && data.definition != null)
        {
            return data.definition.traitType;
        }
        return PrisonerType.None;
    }

    // =========================================================================
    // 통합 스폰 로직 (RiotSystem 제거됨)
    // =========================================================================
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anomalyDatabase == null) return;

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();

        // 1. 현재 방 주인의 타입 확인 (Normal, Nervous, Muscular 등)
        PrisonerType residentType = GetPrisonerType(cellId);

        // 2. 오늘의 범인(Culprit) 선정
        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious && dailyList.Count > 0)
        {
            int rndIndex = UnityEngine.Random.Range(0, dailyList.Count);
            culpritDef = dailyList[rndIndex];
        }

        // 3. DB 순회
        foreach (var def in anomalyDatabase.defs)
        {
            bool isCulprit = (def == culpritDef);
            bool spawnAsNormal = false;

            if (!isCulprit)
            {
                // A. Always Spawn Normal
                if (def.alwaysSpawnNormal) spawnAsNormal = true;

                // B. Individual (죄수 맞춤형 소품)
                // ★ PrisonerType 비교 (RiotGauge 조건은 삭제됨)
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner == residentType)
                {
                    spawnAsNormal = true;
                }
            }

            if (!isCulprit && !spawnAsNormal) continue;

            GameObject prefabToSpawn = isCulprit ? def.suspiciousPrefab : def.normalPrefab;
            if (prefabToSpawn == null) continue;

            // 스폰 실행
            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (anchor.structure != null)
                {
                    GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                    if (defaultObj != null && defaultObj.activeSelf)
                    {
                        defaultObj.SetActive(false);
                        var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);
                        var actor = go.GetComponent<AnomalyActor>();
                        if (actor != null) actor.Init(cellId, def, isCulprit);
                        content.anomalies.Add(go);
                    }
                }
            }
            else
            {
                int slotIndex = availableSlots.FindIndex(s => s.kind == def.kind);
                if (slotIndex != -1)
                {
                    var targetSlot = availableSlots[slotIndex];
                    availableSlots.RemoveAt(slotIndex);
                    var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                    var actor = go.GetComponent<AnomalyActor>();
                    if (actor != null) actor.Init(cellId, def, isCulprit);
                    content.anomalies.Add(go);
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

        // 모든 감방을 순회하며 스폰 실행
        var allCellIds = anchorRegistry.GetAllCellIds();
        foreach (var cellId in allCellIds)
        {
            var role = scheduleManager.GetDailyRole(cellId);
            SpawnForCell(cellId, role.isSuspicious);
        }
    }
}