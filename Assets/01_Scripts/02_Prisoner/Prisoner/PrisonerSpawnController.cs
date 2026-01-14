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

    [Header("Anomaly Database (Must Assign!)")]
    [SerializeField] private AnomalyDatabaseSO anomalyDatabase;

    [Header("Default Settings")]
    [SerializeField] private GameObject defaultPrisonerPrefab;
    [SerializeField] private GameObject cellPropPrefab;

    [Header("Special Spawn Settings")]
    [SerializeField] private Transform[] centerSpawnPoints;
    private int _currentCenterSpawnIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true; // 디버그 로그 켜기

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
        if (!anchorRegistry.TryGet(cellId, out var anchor))
        {
            Debug.LogError($"[Spawn] Cell Anchor not found for {cellId}");
            return;
        }

        // 1. 데이터 가져오기
        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        DailyRoleData dailyRole = scheduleManager.GetDailyRole(cellId); // Struct는 null이 될 수 없음

        if (existingData == null)
        {
            // 죄수가 없는 방은 로그 생략
            return;
        }

        // =========================================================================
        // ★ [DEBUG] 데이터 및 AI 타입 확인 로그 (수정됨)
        // =========================================================================
        if (verboseLog)
        {
            string trait = existingData.definition != null ? existingData.definition.traitType.ToString() : "NULL_DEF";

            // [수정] DailyRoleData는 Struct이므로 null 체크 없이 바로 접근
            string role = dailyRole.dailyAIType.ToString();
            string visual = dailyRole.visualType.ToString();

            Debug.Log($"<color=yellow>[Spawn Check] Cell: {cellId}</color>\n" +
                      $"   - ID: {existingData.ID}\n" +
                      $"   - Trait(AI): {trait}\n" +
                      $"   - DailyRole: {role}\n" +
                      $"   - VisualType: {visual}\n" +
                      $"   - Suspicious: {isSuspicious}");
        }
        // =========================================================================

        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // 프리팹 결정 로직
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;
        GameObject prefabToUse = null;

        if (existingData.definition != null) prefabToUse = existingData.definition.prisonerPrefab;

        if (prefabToUse == null)
        {
            prefabToUse = defaultPrisonerPrefab;
        }

        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            string targetID = dailyRole.visualType.ToString();
            if (prisonerDatabase.TryGet(targetID, out PrisonerDefinition specialDef))
            {
                if (specialDef.prisonerPrefab != null) prefabToUse = specialDef.prisonerPrefab;
            }
        }

        // =========================================================================
        // ★ [DEBUG] 중앙 스폰 로직 및 디버깅
        // =========================================================================
        bool isCenterTarget = IsCenterSpawnTarget(dailyRole.visualType);

        if (isCenterTarget)
        {
            if (centerSpawnPoints == null || centerSpawnPoints.Length == 0)
            {
                Debug.LogError($"[Spawn Error] {cellId}는 중앙 스폰 대상이지만, 'Center Spawn Points' 배열이 비어있습니다!");
            }
            else if (_currentCenterSpawnIndex >= centerSpawnPoints.Length)
            {
                Debug.LogError($"[Spawn Error] 중앙 스폰 자리 부족! (Index: {_currentCenterSpawnIndex}, Max: {centerSpawnPoints.Length}) -> 감방에 소환됨.");
            }
            else
            {
                spawnPos = centerSpawnPoints[_currentCenterSpawnIndex].position;
                spawnRot = centerSpawnPoints[_currentCenterSpawnIndex].rotation;

                if (verboseLog) Debug.Log($"   >> [Center Spawn] {cellId} moved to Center Point {_currentCenterSpawnIndex}");

                _currentCenterSpawnIndex++;
            }
        }
        // =========================================================================

        // 죄수 생성
        PrisonerController controller = InstantiatePrisoner(prefabToUse, spawnPos, spawnRot, anchor, existingData, isSuspicious);

        if (controller == null) return;

        // 생성된 직후 컨트롤러 상태 확인
        if (verboseLog)
        {
            var fsm = controller.GetComponent<PrisonerFSM>();
            Debug.Log($"   -> Controller Initialized. Has FSM: {(fsm != null)}");
        }

        content.prisoner = controller;

        // 프롭 생성
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            content.prop = propGo;
        }

        // 이상현상 소환
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    private PrisonerController InstantiatePrisoner(GameObject prefab, Vector3 pos, Quaternion rot, CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (prefab == null) return null;
        var pGo = Instantiate(prefab, pos, rot);
        pGo.name = $"Prisoner_{data.ID}";
        var controller = pGo.GetComponent<PrisonerController>();
        if (controller == null) Debug.LogError($"[Spawn] Prefab '{prefab.name}' has no PrisonerController!");
        else controller.Initialize(data, anchor, isSuspicious);

        var dialogue = pGo.GetComponent<PrisonerDialogue>();
        if (dialogue != null)
        {
            // Manager에서 현재 방에 할당된 DailyRole을 가져와서 직접 넣어줌
            var dailyRole = scheduleManager.GetDailyRole(anchor.cellId);
            dialogue.myVisualType = dailyRole.visualType;
        }

        return controller;
    }

    private bool IsCenterSpawnTarget(VisualAnomalyType type)
    {
        switch (type)
        {
            case VisualAnomalyType.Imposter_Guard:
            case VisualAnomalyType.Imposter_NoBeard:
            case VisualAnomalyType.Imposter_Earring:
            case VisualAnomalyType.Suspect1:
            case VisualAnomalyType.Suspect2:
            case VisualAnomalyType.Suspect3:

                return true;
            default:
                return false;
        }
    }

    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        // Anomaly 관련 로그는 제거함

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();
        AnomalyDefinitionSO culpritDef = null;

        if (isSuspicious && dailyList.Count > 0)
        {
            int rndIndex = UnityEngine.Random.Range(0, dailyList.Count);
            culpritDef = dailyList[rndIndex];
        }

        // PASS 1: Daily Anomalies
        foreach (var def in dailyList)
        {
            bool isRealAnomaly = (def == culpritDef);
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;
            bool shouldSpawn = isRealAnomaly || def.alwaysSpawnNormal;

            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (shouldSpawn && anchor.structure != null)
                {
                    GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                    if (defaultObj != null && prefabToSpawn != null)
                    {
                        defaultObj.SetActive(false);
                        var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);
                        var actor = go.GetComponent<AnomalyActor>();
                        if (actor != null) actor.Init(cellId, def, isRealAnomaly);
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

                    if (shouldSpawn && prefabToSpawn != null)
                    {
                        var go = Instantiate(prefabToSpawn, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                        var actor = go.GetComponent<AnomalyActor>();
                        if (actor != null) actor.Init(cellId, def, isRealAnomaly);
                        content.anomalies.Add(go);
                    }
                }
            }
        }

        // PASS 2: Fillers
        if (anomalyDatabase != null)
        {
            foreach (var def in anomalyDatabase.defs)
            {
                if (dailyList.Contains(def)) continue;
                if (!def.alwaysSpawnNormal) continue;
                if (def.targetType != AnomalyTargetType.Slot) continue;

                int slotIndex = availableSlots.FindIndex(s => s.kind == def.kind);
                if (slotIndex != -1)
                {
                    var targetSlot = availableSlots[slotIndex];
                    availableSlots.RemoveAt(slotIndex);

                    if (def.normalPrefab != null)
                    {
                        var go = Instantiate(def.normalPrefab, targetSlot.transform.position, targetSlot.transform.rotation, targetSlot.transform);
                        var actor = go.GetComponent<AnomalyActor>();
                        if (actor != null) actor.Init(cellId, def, false);
                        content.anomalies.Add(go);
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
        if (prisonerDatabase == null) Debug.LogError("PrisonerDatabase Missing");
        if (anchorRegistry == null) Debug.LogError("AnchorRegistry Missing");
        if (contentRegistry == null) Debug.LogError("ContentRegistry Missing");
        if (scheduleManager == null) Debug.LogError("ScheduleManager Missing");

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