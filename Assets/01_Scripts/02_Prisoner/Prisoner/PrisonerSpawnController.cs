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

    [Header("Default Settings")]
    [SerializeField] private GameObject defaultPrisonerPrefab; // 일반 죄수 프리팹
    [SerializeField] private GameObject cellPropPrefab;

    [Header("Special Spawn Settings")]
    [SerializeField] private Transform[] centerSpawnPoints;
    private int _currentCenterSpawnIndex = 0;

    [Header("Debug")]
    [SerializeField] private bool verboseLog;

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

        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        DailyRoleData dailyRole = scheduleManager.GetDailyRole(cellId);

        if (existingData == null) return;

        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // =================================================================
        // [핵심 로직] 프리팹 결정 (Prefab Selection)
        // =================================================================
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;

        // 1. 기본은 Default 프리팹
        GameObject prefabToUse = defaultPrisonerPrefab;

        // 2. 특수 외형(Enum)이 있다면 DB에서 검색하여 프리팹 교체
        // ★ 전제조건: PrisonerDatabaseSO에 Enum 이름과 동일한 ID("Imposter_Guard" 등)를 가진 데이터가 있어야 함.
        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            string targetID = dailyRole.visualType.ToString();

            if (prisonerDatabase.TryGet(targetID, out PrisonerDefinition specialDef))
            {
                if (specialDef.prisonerPrefab != null)
                {
                    prefabToUse = specialDef.prisonerPrefab;
                    if (verboseLog) Debug.Log($"[Spawn] {cellId} visual changed to {targetID}");
                }
                else
                {
                    Debug.LogWarning($"[Spawn] ID '{targetID}' 데이터는 찾았으나 Prefab이 비어있습니다. Default를 사용합니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[Spawn] VisualType '{targetID}'에 해당하는 데이터를 DB에서 찾지 못했습니다. (ID 불일치?) Default를 사용합니다.");
            }
        }

        // 3. 4일차 중앙 스폰 타겟 확인 (위치 덮어쓰기)
        if (IsCenterSpawnTarget(dailyRole.visualType))
        {
            if (centerSpawnPoints != null && _currentCenterSpawnIndex < centerSpawnPoints.Length)
            {
                spawnPos = centerSpawnPoints[_currentCenterSpawnIndex].position;
                spawnRot = centerSpawnPoints[_currentCenterSpawnIndex].rotation;
                _currentCenterSpawnIndex++;
            }
        }

        // 4. 결정된 프리팹으로 인스턴스화
        PrisonerController controller = InstantiatePrisoner(prefabToUse, spawnPos, spawnRot, anchor, existingData, isSuspicious);
        content.prisoner = controller;

        // 5. 방 프롭 생성 (망치, 책상 위 물건 등)
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
        }

        // 6. 이상현상 소환
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    // Helper
    private PrisonerController InstantiatePrisoner(GameObject prefab, Vector3 pos, Quaternion rot, CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (prefab == null) return null;

        var pGo = Instantiate(prefab, pos, rot);
        pGo.name = $"Prisoner_{data.ID}";

        var controller = pGo.GetComponent<PrisonerController>();

        // 프리팹에 Controller가 안 붙어있으면 에러
        if (controller == null)
        {
            Debug.LogError($"[Spawn] Prefab '{prefab.name}'에 PrisonerController 컴포넌트가 없습니다!");
            return null;
        }

        controller.Initialize(data, anchor, isSuspicious);
        return controller;
    }

    // ... (나머지 로직 동일) ...
    private bool IsCenterSpawnTarget(VisualAnomalyType type)
    {
        switch (type)
        {
            case VisualAnomalyType.Imposter_Guard:
            case VisualAnomalyType.Imposter_NoBeard:
            case VisualAnomalyType.Imposter_Earring:
                return true;
            default:
                return false;
        }
    }

    // ... SpawnAnomaliesLogic, HandleSuppressStart 등 기존 로직 유지 ...
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        // (기존 코드 그대로 사용)
        if (anchor.currentDailyAnomalies == null || anchor.currentDailyAnomalies.Count == 0) return;
        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        AnomalyDefinitionSO culpritDef = null;

        if (isSuspicious)
        {
            int rndIndex = UnityEngine.Random.Range(0, anchor.currentDailyAnomalies.Count);
            culpritDef = anchor.currentDailyAnomalies[rndIndex];
        }

        foreach (var def in anchor.currentDailyAnomalies)
        {
            bool isRealAnomaly = (def == culpritDef);
            GameObject prefabToSpawn = isRealAnomaly ? def.suspiciousPrefab : def.normalPrefab;

            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (isRealAnomaly || def.alwaysSpawnNormal)
                {
                    if (anchor.structure != null && prefabToSpawn != null)
                    {
                        GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                        if (defaultObj != null)
                        {
                            defaultObj.SetActive(false);
                            var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);
                            var actor = go.GetComponent<AnomalyActor>();
                            if (actor != null) actor.Init(cellId, def, isRealAnomaly);
                            content.anomalies.Add(go);
                        }
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

    private bool ValidateRefs()
    {
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null && defaultPrisonerPrefab != null && scheduleManager != null;
    }

    public void SpawnAllPrisoners()
    {
        // (기존 코드 그대로 사용)
        if (anchorRegistry == null) return;
        var allCellIds = anchorRegistry.GetAllCellIds();
        foreach (var cellId in allCellIds)
        {
            var role = scheduleManager.GetDailyRole(cellId);
            SpawnForCell(cellId, role.isSuspicious);
        }
    }
}