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

        // 1. 데이터 가져오기
        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        DailyRoleData dailyRole = scheduleManager.GetDailyRole(cellId);

        if (existingData == null) return;

        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // =================================================================
        // ★ [수정됨] 프리팹 결정 로직 (Data Priority)
        // =================================================================
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;

        // A. 일단 '이 죄수의 원래 데이터'에 있는 프리팹을 가져옵니다.
        GameObject prefabToUse = null;

        if (existingData.definition != null)
        {
            prefabToUse = existingData.definition.prisonerPrefab;
        }

        // B. 만약 데이터에 프리팹이 없다면? -> Inspector의 Default로 땜빵 (안전장치)
        if (prefabToUse == null)
        {
            prefabToUse = defaultPrisonerPrefab;
            if (verboseLog) Debug.LogWarning($"[Spawn] {cellId} 죄수의 데이터에 프리팹이 없어 Default를 사용합니다.");
        }

        // C. 오늘 '특수 외형(Imposter 등)'이 배정되었다면? -> 덮어쓰기 (Override)
        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            string targetID = dailyRole.visualType.ToString();

            if (prisonerDatabase.TryGet(targetID, out PrisonerDefinition specialDef))
            {
                if (specialDef.prisonerPrefab != null)
                {
                    prefabToUse = specialDef.prisonerPrefab;
                    if (verboseLog) Debug.Log($"[Spawn] {cellId} ({existingData.definition?.templateId})가 {targetID}로 변장했습니다.");
                }
            }
        }

        // -----------------------------------------------------------------
        // [이하 동일] 위치 결정 및 소환
        // -----------------------------------------------------------------

        // 4일차 중앙 스폰 타겟 확인
        if (IsCenterSpawnTarget(dailyRole.visualType))
        {
            if (centerSpawnPoints != null && _currentCenterSpawnIndex < centerSpawnPoints.Length)
            {
                spawnPos = centerSpawnPoints[_currentCenterSpawnIndex].position;
                spawnRot = centerSpawnPoints[_currentCenterSpawnIndex].rotation;
                _currentCenterSpawnIndex++;
            }
        }

        // 실제 생성
        PrisonerController controller = InstantiatePrisoner(prefabToUse, spawnPos, spawnRot, anchor, existingData, isSuspicious);

        if (controller == null) return;

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