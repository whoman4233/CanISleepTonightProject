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

    [Header("Prisoner Prefab")]
    [SerializeField] private GameObject defaultPrisonerPrefab;

    [Header("Cell Prop")]
    [SerializeField] private GameObject cellPropPrefab;

    [Header("Special Spawn Settings")]
    [SerializeField] private Transform[] centerSpawnPoints; // 중앙 스폰 위치 배열 (씬에서 할당)
    // [Deleted] VisualAnomalyDatabaseSO는 제거됨 (PrisonerDatabaseSO 사용)

    private int _currentCenterSpawnIndex = 0;

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
        _currentCenterSpawnIndex = 0; // 하루 시작 시 중앙 스폰 인덱스 초기화

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

        // 1. 데이터 가져오기
        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        DailyRoleData dailyRole = scheduleManager.GetDailyRole(cellId); // 오늘의 역할(VisualType 포함)

        if (existingData == null)
        {
            if (verboseLog) Debug.LogWarning($"[Spawn] No prisoner active for {cellId} today.");
            return;
        }

        // 2. 컨텐츠 컨테이너 생성
        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // =================================================================
        // ★ [핵심 로직] 위치 및 프리팹 결정
        // =================================================================
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;
        GameObject prefabToUse = defaultPrisonerPrefab;


        // A. [프리팹 교체] 특수 외형(Enum)이 설정되어 있다면 DB에서 검색
        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            // Enum 이름을 문자열로 변환 (예: Imposter_Guard -> "Imposter_Guard")
            string targetID = dailyRole.visualType.ToString();

            // DB에서 같은 ID를 가진 죄수 데이터(프리팹) 찾기
            if (prisonerDatabase.TryGet(targetID, out PrisonerDefinition specialDef))
            {
                if (specialDef.prisonerPrefab != null)
                {
                    prefabToUse = specialDef.prisonerPrefab;
                    if (verboseLog) Debug.Log($"[Spawn] {cellId} visual changed to {targetID}");
                }
            }
            else
            {
                Debug.LogWarning($"[Spawn] VisualType '{targetID}'에 해당하는 데이터를 PrisonerDatabase에서 찾을 수 없습니다. (ID 일치 확인 필요)");
            }
        }

        // B. [위치 변경] 4일차 사수 찾기 타겟이라면 중앙으로 납치
        if (IsCenterSpawnTarget(dailyRole.visualType))
        {
            if (centerSpawnPoints != null && _currentCenterSpawnIndex < centerSpawnPoints.Length)
            {
                spawnPos = centerSpawnPoints[_currentCenterSpawnIndex].position;
                spawnRot = centerSpawnPoints[_currentCenterSpawnIndex].rotation;
                _currentCenterSpawnIndex++;

                if (verboseLog) Debug.Log($"[Spawn] {cellId} moved to Center Spawn Index {_currentCenterSpawnIndex - 1}");
            }
            else
            {
                Debug.LogWarning($"[Spawn] Center Spawn Points missing or full! Spawning {cellId} in cell as fallback.");
            }
        }

        // 3. 죄수 실제 생성
        PrisonerController controller = InstantiatePrisoner(prefabToUse, spawnPos, spawnRot, anchor, existingData, isSuspicious);
        content.prisoner = controller;

        // 4. 프롭 생성 (중앙 스폰이어도 감방 내 프롭은 생성)
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            propGo.name = $"Prop_{cellId}";
            content.prop = propGo;
        }

        // 5. 이상현상 소환 실행
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    // -----------------------------------------------------------------------
    // Helper & Logic
    // -----------------------------------------------------------------------

    // ★ 4일차 미션 타겟(중앙 스폰 대상) 판별
    private bool IsCenterSpawnTarget(VisualAnomalyType type)
    {
        switch (type)
        {
            // 여기에 중앙에 소환하고 싶은 타입만 나열하세요
            case VisualAnomalyType.Imposter_Guard:
            case VisualAnomalyType.Imposter_NoBeard:
            case VisualAnomalyType.Imposter_Earring:
                return true;

            // 그 외(Bikini 등)는 false -> 자기 방 스폰
            default:
                return false;
        }
    }

    private PrisonerController InstantiatePrisoner(GameObject prefab, Vector3 pos, Quaternion rot, CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (prefab == null) return null;

        var pGo = Instantiate(prefab, pos, rot);
        pGo.name = $"Prisoner_{data.ID}";

        var controller = pGo.GetComponent<PrisonerController>();
        if (controller == null) controller = pGo.AddComponent<PrisonerController>();

        controller.Initialize(data, anchor, isSuspicious);
        return controller;
    }

    // -----------------------------------------------------------------------
    // 기존 내부 로직 유지
    // -----------------------------------------------------------------------

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
        // visualDatabase 검사 제거됨
        return prisonerDatabase != null && anchorRegistry != null && contentRegistry != null &&
               defaultPrisonerPrefab != null && scheduleManager != null;
    }

    public void SpawnAllPrisoners()
    {
        if (anchorRegistry == null)
        {
            Debug.LogError("[SpawnController] AnchorRegistry가 연결되지 않았습니다.");
            return;
        }

        // 1. 모든 방의 ID 목록 가져오기
        var allCellIds = anchorRegistry.GetAllCellIds();

        foreach (var cellId in allCellIds)
        {
            // 2. 이 죄수가 오늘 범인(Suspicious)인지 확인
            // (ScheduleManager가 역할을 알고 있음)
            var role = scheduleManager.GetDailyRole(cellId);

            // 3. 개별 스폰 실행
            // (이 안에서 4일차 중앙 스폰 / 7일차 땅파기 등 로직이 수행됨)
            SpawnForCell(cellId, role.isSuspicious);
        }

        if (verboseLog) Debug.Log($"[Spawn] 총 {allCellIds.Count}명의 죄수 스폰 시도 완료.");
    }
}