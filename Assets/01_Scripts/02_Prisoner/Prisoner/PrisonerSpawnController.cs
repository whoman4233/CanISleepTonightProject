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

    // ▼ [추가] Mission 3 전용 프리팹 연결 변수
    [Header("Mission 3 Assets")]
    [SerializeField] private GameObject graffitiPrefab;
    [SerializeField] private GameObject goatHeadPrefab;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private void OnEnable() => PrisonerEventBus.OnSuppressSessionStarted += HandleSuppressStart;
    private void OnDisable() => PrisonerEventBus.OnSuppressSessionStarted -= HandleSuppressStart;

    public void ClearAllForNewDay()
    {
        _currentCenterSpawnIndex = 0;

        if (contentRegistry != null)
        {
            if (anchorRegistry != null)
            {
                foreach (var cellId in anchorRegistry.GetAllCellIds())
                {
                    if (contentRegistry.TryGet(cellId, out var content))
                    {
                        if (content.prisoner != null) Destroy(content.prisoner.gameObject);
                        if (content.prop != null) Destroy(content.prop);

                        if (content.anomalies != null)
                        {
                            foreach (var anomaly in content.anomalies)
                            {
                                if (anomaly != null) Destroy(anomaly);
                            }
                        }
                    }
                }
            }
            contentRegistry.ClearAll();
        }

        if (anchorRegistry != null)
        {
            foreach (var anchor in anchorRegistry.GetAllAnchors())
            {
                if (anchor != null)
                {
                    if (anchor.structure != null)
                    {
                        anchor.structure.ResetAllDefaults();
                        anchor.IsOccupied = false;
                    }

                    if (anchor.prisonerSpawn != null)
                    {
                        foreach (Transform child in anchor.prisonerSpawn)
                        {
                            if (child != null) Destroy(child.gameObject);
                        }
                    }
                }
            }
        }

        PrisonerController[] allPrisoners = UnityEngine.Object.FindObjectsOfType<PrisonerController>();
        foreach (var prisoner in allPrisoners)
        {
            if (prisoner != null && prisoner.gameObject != null)
            {
                Destroy(prisoner.gameObject);
            }
        }

        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Frank") && obj.scene.isLoaded)
            {
                if (obj.CompareTag("Player") || obj.GetComponent<GameManager>() != null) continue;
                Destroy(obj);
            }
        }

        Debug.Log("🧹 [System] Registry 정리 및 씬 내 잔여 죄수/Frank를 완벽하게 소거했습니다.");
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

        // ------------------------------------------------------------
        // 프리팹 선정
        // ------------------------------------------------------------
        GameObject prefabToUse = null;
        string prefabSource = "";

        if (existingData.definition != null && existingData.definition.prisonerPrefab != null)
        {
            prefabToUse = existingData.definition.prisonerPrefab;
            prefabSource = "Original_SO";
        }

        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            string targetID = dailyRole.visualType.ToString();
            PrisonerDefinition specialDef = null;

            if (!prisonerDatabase.TryGet(targetID, out specialDef))
            {
                prisonerDatabase.TryGet("PSN_" + targetID, out specialDef);
            }

            if (specialDef != null && specialDef.prisonerPrefab != null)
            {
                prefabToUse = specialDef.prisonerPrefab;
                prefabSource = $"Override_({targetID})";
            }
            else
            {
                Debug.LogWarning($"[Spawn] '{targetID}' (또는 PSN_{targetID})를 DB에서 찾을 수 없어 기본 죄수를 소환합니다.");
            }
        }

        if (prefabToUse == null)
        {
            prefabToUse = defaultPrisonerPrefab;
            prefabSource = "FALLBACK_DEFAULT";
        }

        // ================================================================
        // 📍 [위치] 위치 선정 로직
        // ================================================================
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;
        string locationLog = "MainSpawn";

        if (anchor.randomSpawnPoints != null && anchor.randomSpawnPoints.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, anchor.randomSpawnPoints.Count);
            Transform randomPoint = anchor.randomSpawnPoints[randomIndex];

            if (randomPoint != null)
            {
                spawnPos = randomPoint.position;
                spawnRot = randomPoint.rotation;
                locationLog = $"Random[{randomIndex}]";
            }
        }

        bool isCenterTarget = IsCenterSpawnTarget(dailyRole.visualType);

        if (isCenterTarget)
        {
            if (centerSpawnPoints != null && _currentCenterSpawnIndex < centerSpawnPoints.Length)
            {
                spawnPos = centerSpawnPoints[_currentCenterSpawnIndex].position;
                spawnRot = centerSpawnPoints[_currentCenterSpawnIndex].rotation;
                _currentCenterSpawnIndex++;
                locationLog = $"CENTER[{_currentCenterSpawnIndex - 1}]";
            }
            else
            {
                locationLog = "CENTER_FAIL(Full)";
            }
        }

        if (verboseLog)
        {
            Debug.Log($"[{cellId}] {existingData.ID} | Role: {dailyRole.visualType} | Pos: {locationLog} | Prefab: {prefabToUse.name} ({prefabSource})");
        }

        // 4. 죄수 생성
        PrisonerController controller = InstantiatePrisoner(prefabToUse, spawnPos, spawnRot, anchor, existingData, isSuspicious);
        if (controller != null) content.prisoner = controller;

        // ▼ [추가] Mission 3 전용 프랍 소환 로직
        if (DailyMissionManager.Instance.CurrentMission != null && DailyMissionManager.Instance.CurrentMission.missionId == "Mission03")
        {
            // Case 1: 낙서범 (Graffiti) -> 방 정중앙 (Cell Anchor 위치)
            if (existingData.RuntimeAIType == PrisonerAIType.Graffiti)
            {
                if (graffitiPrefab != null)
                {
                    Instantiate(graffitiPrefab, anchor.transform.position, anchor.transform.rotation);
                    Debug.Log($"[Mission3] {cellId}에 그래피티 소환됨 (AIType : Graffiti)");
                }
            }
            // Case 2: 염소 머리 (GoatHead) -> 죄수 스폰 위치 (점호 위치)
            if (dailyRole.visualType == VisualAnomalyType.GoatHead)
            {
                if (goatHeadPrefab != null)
                {
                    Instantiate(goatHeadPrefab, spawnPos, spawnRot);
                    Debug.Log($"[Mission3] {cellId}에 염소 머리 소환됨 (Visual: GoatHead)");
                }
            }
        }

        // 5. 기본 프롭 생성
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            content.prop = propGo;
        }

        // 6. 이상현상 스폰
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anomalyDatabase == null) return;

        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        HashSet<AnomalyTargetType> processedReplacements = new HashSet<AnomalyTargetType>();

        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();
        AnomalyDefinitionSO culpritDef = (dailyList.Count > 0) ? dailyList[0] : null;

        if (culpritDef != null && isSuspicious)
        {
            TrySpawnSingleAnomaly(cellId, culpritDef, anchor, availableSlots, processedReplacements, content, true);
        }

        PrisonerType residentType = GetPrisonerType(cellId);

        foreach (var def in anomalyDatabase.defs)
        {
            if (def == null) continue;
            if (def == culpritDef) continue;

            bool shouldSpawn = false;

            if (def.alwaysSpawnNormal || def.isDecorative)
            {
                bool typeMatch = true;
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner != residentType)
                    typeMatch = false;

                if (typeMatch) shouldSpawn = true;
            }

            if (shouldSpawn)
            {
                TrySpawnSingleAnomaly(cellId, def, anchor, availableSlots, processedReplacements, content, false);
            }
        }
    }

    private void TrySpawnSingleAnomaly(string cellId, AnomalyDefinitionSO def, CellAnchor anchor,
        List<AnomalySpawnSlot> availableSlots, HashSet<AnomalyTargetType> processedReplacements,
        CellContentRegistry.CellContent content, bool isCulprit)
    {
        GameObject prefabToSpawn = isCulprit ? def.suspiciousPrefab : def.normalPrefab;
        if (prefabToSpawn == null) return;

        if (def.targetType != AnomalyTargetType.Slot && processedReplacements.Contains(def.targetType)) return;

        GameObject spawnedGO = null;

        if (def.targetType != AnomalyTargetType.Slot)
        {
            if (anchor.structure != null)
            {
                GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);
                if (defaultObj != null)
                {
                    defaultObj.SetActive(false);
                    spawnedGO = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);
                    processedReplacements.Add(def.targetType);
                }
            }
        }
        else
        {
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
            if (spawnedGO.GetComponent<PrisonerController>() != null)
            {
                Debug.LogError($"[Spawn Error] {def.name}에 Prisoner 컴포넌트가 있습니다! SO 설정을 확인하세요.");
            }

            var actor = spawnedGO.GetComponent<AnomalyActor>();
            if (actor != null) actor.Init(cellId, def, isCulprit);
            content.anomalies.Add(spawnedGO);
        }
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
        string typeStr = type.ToString();
        return typeStr.StartsWith("PSN_Franke") || typeStr.StartsWith("Suspect");
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