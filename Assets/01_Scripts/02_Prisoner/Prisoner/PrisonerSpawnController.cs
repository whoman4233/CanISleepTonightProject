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

        // =========================================================================
        // 1단계: 장부(Registry) 기반의 정석적인 삭제 (데이터 정합성 유지)
        // =========================================================================
        if (contentRegistry != null)
        {
            // 1-1. 레지스트리에 등록된 모든 죄수/소품 삭제
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
            // 1-2. 장부 데이터 초기화
            contentRegistry.ClearAll();
        }

        // 1-3. 앵커(방) 상태 및 하위 오브젝트 초기화
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

                    // 방 스폰 위치 하위에 붙어있는 잔여물 강제 삭제
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

        // =========================================================================
        // 2단계: 씬(Scene) 전수 조사 (★ Frank 삭제 불가 문제 해결의 핵심)
        // 장부에 등록되지 않았더라도, 씬에 존재하는 모든 '죄수' 스크립트를 찾아 파괴함
        // =========================================================================

        // 1. 죄수 컨트롤러(PrisonerController)가 붙은 모든 오브젝트 찾기
        PrisonerController[] allPrisoners = UnityEngine.Object.FindObjectsOfType<PrisonerController>();
        foreach (var prisoner in allPrisoners)
        {
            if (prisoner != null && prisoner.gameObject != null)
            {
                // 이미 위에서 Destroy 되었을 수도 있으므로 체크 후 파괴
                Destroy(prisoner.gameObject);
            }
        }

        // 2. (옵션) 만약 Frank가 죄수 스크립트가 없는 특수 NPC라면, 이름으로 확인사살
        // "FrankR" 이라는 이름이 포함된 게임 오브젝트를 찾아서 파괴
        // (주의: 씬 구조에 따라 비효율적일 수 있으나 확실함)
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        foreach (var obj in allObjects)
        {
            // 이름에 Frank가 포함되어 있고, 아직 안 죽었다면 (Player나 필수 시스템 제외)
            if (obj.name.Contains("Frank") && obj.scene.isLoaded)
            {
                // Player나 매니저는 지우면 안됨 (필터링)
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

        // 1. 데이터 가져오기
        PrisonerData existingData = scheduleManager.GetPrisonerData(cellId);
        DailyRoleData dailyRole = scheduleManager.GetDailyRole(cellId); // ★ 여기서 역할이 제대로 들어왔는지 확인해야 함

        if (existingData == null) return;

        var content = new CellContentRegistry.CellContent();
        content.prisonerInstanceId = existingData.ID;

        // ================================================================
        // 🕵️‍♂️ [현황판] 프리팹 선정 과정 추적
        // ================================================================
        GameObject prefabToUse = null;
        string prefabSource = ""; // 프리팹이 어디서 왔는지 출처 기록

        // [1단계] 죄수 고유 데이터(SO) 확인
        if (existingData.definition != null && existingData.definition.prisonerPrefab != null)
        {
            prefabToUse = existingData.definition.prisonerPrefab;
            prefabSource = "Original_SO";
        }

        // [2단계] 역할(VisualType)에 따른 오버라이드 (미션 등)
        if (dailyRole.visualType != VisualAnomalyType.None)
        {
            string targetID = dailyRole.visualType.ToString();
            if (prisonerDatabase.TryGet(targetID, out PrisonerDefinition specialDef))
            {
                if (specialDef.prisonerPrefab != null)
                {
                    prefabToUse = specialDef.prisonerPrefab;
                    prefabSource = $"Override_({dailyRole.visualType})";
                }
            }
        }

        // [3단계] 여전히 없으면 기본값 (사용자님 말씀대로면 이 단계가 실행되면 안 됨!)
        if (prefabToUse == null)
        {
            prefabToUse = defaultPrisonerPrefab;
            prefabSource = "FALLBACK_DEFAULT"; // 여기가 범인일 가능성 높음
        }


        // ================================================================
        // 📍 [위치] 위치 선정 로직 추적
        // ================================================================
        Vector3 spawnPos = anchor.prisonerSpawn.position;
        Quaternion spawnRot = anchor.prisonerSpawn.rotation;
        bool isCenterTarget = IsCenterSpawnTarget(dailyRole.visualType);
        string locationLog = "Room";

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

        // ================================================================
        // 📢 [최종 로그] 파싱 결과 출력
        // ================================================================
        if (verboseLog)
        {
            // 로그 형식: [방번호] 죄수ID | 역할 | 소환위치 | 최종프리팹(출처)
            Debug.Log($"[{cellId}] {existingData.ID} | Role: {dailyRole.visualType} | Pos: {locationLog} | Prefab: {prefabToUse.name} ({prefabSource})");
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

        // 6. 이상현상 스폰
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    private PrisonerController InstantiatePrisoner(GameObject prefab, Vector3 pos, Quaternion rot, CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (prefab == null) return null;
        var pGo = Instantiate(prefab, pos, rot);
        pGo.name = $"Prisoner_{data.ID}"; // 이름 통일

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
        // ★ [중요] 실수로 엉뚱한 놈이 중앙에 안 오도록 조건 확인
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

    // =========================================================================
    // ★ [정리된 버전] 불필요한 로그 삭제 + 이상현상 생성만 집중
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
            string spawnSource = ""; // 디버깅용 출처 태그

            // 1. 범인 처리
            if (isCulprit)
            {
                prefabToSpawn = def.suspiciousPrefab;
                spawnSource = "Culprit";
            }
            // 2. AlwaysSpawnNormal 처리
            else if (def.alwaysSpawnNormal)
            {
                bool typeMatch = true;
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner != residentType)
                    typeMatch = false;

                if (typeMatch)
                {
                    prefabToSpawn = def.normalPrefab;
                    spawnSource = "Always";
                }
            }
            // 3. IsDecorative 처리
            else if (def.isDecorative)
            {
                bool typeMatch = true;
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner != residentType)
                    typeMatch = false;

                if (typeMatch)
                {
                    prefabToSpawn = def.normalPrefab;
                    spawnSource = "Deco";
                }
            }

            if (prefabToSpawn == null) continue;

            // 중복 및 슬롯 검사
            if (def.targetType != AnomalyTargetType.Slot && processedReplacements.Contains(def.targetType)) continue;

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
                // ★ [문제 2 추적] 이상현상인데 "사람"이 소환됐는지 체크 (컴포넌트 검사)
                if (spawnedGO.GetComponent<PrisonerController>() != null)
                {
                    Debug.LogError($" [CRITICAL ERROR] {cellId}에 소환된 이상현상 '{def.name}'({spawnSource})이 '사람(Prisoner)' 프리팹을 담고 있습니다! SO를 확인하세요.");
                }

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