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
        // ★ [DEBUG] 데이터 및 AI 타입 확인 로그
        // =========================================================================
        if (verboseLog)
        {
            string trait = existingData.definition != null ? existingData.definition.traitType.ToString() : "NULL_DEF";
            string role = dailyRole.dailyAIType.ToString();
            string visual = dailyRole.visualType.ToString();

            Debug.Log($"<color=yellow>[Spawn Check] Cell: {cellId}</color>\n" +
                      $"   - ID: {existingData.ID}\n" +
                      $"   - Trait(AI): {trait}\n" +
                      $"   - DailyRole: {role}\n" +
                      $"   - VisualType: {visual}\n" +
                      $"   - Suspicious: {isSuspicious}");
        }

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
        // ★ [DEBUG] 중앙 스폰 로직
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

        // 프롭 생성 (기존 로직 유지)
        if (cellPropPrefab != null && anchor.propSpawnPoint != null)
        {
            var propGo = Instantiate(cellPropPrefab, anchor.propSpawnPoint.position, anchor.propSpawnPoint.rotation, anchor.transform);
            content.prop = propGo;
        }

        // 이상현상(및 죄수 전용 소품) 소환 - 로직 개편됨
        SpawnAnomaliesLogic(cellId, anchor, isSuspicious, content);

        contentRegistry.Set(cellId, content);
        anchor.IsOccupied = true;
    }

    private PrisonerController InstantiatePrisoner(GameObject prefab, Vector3 pos, Quaternion rot, CellAnchor anchor, PrisonerData data, bool isSuspicious)
    {
        if (prefab == null) return null;
        var pGo = Instantiate(prefab, pos, rot);
        pGo.name = $"Prisoner_{data.ID}";

        // [추가] 레이어 강제 설정 (WeaponHitbox 충돌 문제 방지)
        int prisonerLayer = LayerMask.NameToLayer("Prisoner");
        if (prisonerLayer != -1) SetLayerRecursively(pGo, prisonerLayer);

        var controller = pGo.GetComponent<PrisonerController>();
        if (controller == null) Debug.LogError($"[Spawn] Prefab '{prefab.name}' has no PrisonerController!");
        else controller.Initialize(data, anchor, isSuspicious);

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

    // ★ [추가] 죄수의 원래 성격(Trait)을 가져오는 헬퍼 함수
    private PrisonerTraitType GetPrisonerTrait(string cellId)
    {
        var data = scheduleManager.GetPrisonerData(cellId);
        // PrisonerData의 definition이 null이 아닐 때 traitType 반환
        if (data != null && data.definition != null)
        {
            return data.definition.traitType;
        }
        return PrisonerTraitType.Normal; // 기본값
    }

    // =========================================================================
    // ★ [핵심 수정] 이상현상 + 개별 소품 통합 스폰 로직
    // =========================================================================
    private void SpawnAnomaliesLogic(string cellId, CellAnchor anchor, bool isSuspicious, CellContentRegistry.CellContent content)
    {
        if (anomalyDatabase == null) return;

        // 1. 슬롯 및 데이터 준비
        List<AnomalySpawnSlot> availableSlots = new List<AnomalySpawnSlot>(anchor.anomalySlots);
        List<AnomalyDefinitionSO> dailyList = anchor.currentDailyAnomalies ?? new List<AnomalyDefinitionSO>();

        // 죄수 성격 파악 (개별 소품용)
        PrisonerTraitType residentTrait = GetPrisonerTrait(cellId);

        // 폭동 게이지 파악 (특수 소품용 - GameManager가 있을 경우)
        int currentRiotGauge = GameManager.Instance != null ? GameManager.Instance.RiotGauge : 0;

        // 2. 오늘의 '진짜 이상현상(Culprit)' 선정
        AnomalyDefinitionSO culpritDef = null;
        if (isSuspicious && dailyList.Count > 0)
        {
            int rndIndex = UnityEngine.Random.Range(0, dailyList.Count);
            culpritDef = dailyList[rndIndex];
        }

        // 3. 전체 DB 순회하며 스폰 여부 결정
        foreach (var def in anomalyDatabase.defs)
        {
            // A. 소환 조건 판단
            bool isCulprit = (def == culpritDef); // 오늘의 범인인가?
            bool spawnAsNormal = false;           // 정상 프리팹으로 소환할 것인가?

            if (isCulprit)
            {
                // 범인이면 무조건 SuspiciousPrefab 소환 대상
            }
            else
            {
                // 범인이 아니라면 NormalPrefab 소환 조건 체크

                // 1) 항상 등장 옵션 (벽시계 등)
                if (def.alwaysSpawnNormal) spawnAsNormal = true;

                // 2) [죄수 전용] 카테고리가 Individual이고, 죄수 성격과 일치 (성경, 덤벨 등)
                if (def.category == AnomalyCategory.Individual && def.targetPrisoner == residentTrait)
                {
                    spawnAsNormal = true;
                }

                // 3) [특수] 카테고리가 Special이고, 폭동 게이지 조건 충족
                if (def.category == AnomalyCategory.Special && currentRiotGauge >= def.minRiotGauge)
                {
                    spawnAsNormal = true;
                }
            }

            // 소환 대상이 아니면 패스
            if (!isCulprit && !spawnAsNormal) continue;

            // B. 프리팹 결정
            GameObject prefabToSpawn = isCulprit ? def.suspiciousPrefab : def.normalPrefab;
            if (prefabToSpawn == null) continue;

            // C. 소환 실행 (구조물 교체 vs 슬롯 배치)
            if (def.targetType != AnomalyTargetType.Slot)
            {
                // [구조물 교체형] - 기존 가구 끄고 생성
                if (anchor.structure != null)
                {
                    GameObject defaultObj = anchor.structure.GetDefaultObject(def.targetType);

                    // 해당 위치가 아직 활성 상태라면 교체 (다른 이상현상이 먼저 차지하지 않았을 때)
                    if (defaultObj != null && defaultObj.activeSelf)
                    {
                        defaultObj.SetActive(false); // 기존 구조물 숨기기

                        var go = Instantiate(prefabToSpawn, defaultObj.transform.position, defaultObj.transform.rotation, defaultObj.transform.parent);

                        var actor = go.GetComponent<AnomalyActor>();
                        if (actor != null) actor.Init(cellId, def, isCulprit);

                        content.anomalies.Add(go);
                    }
                }
            }
            else
            {
                // [슬롯 배치형] - 빈 슬롯 찾아서 생성
                int slotIndex = availableSlots.FindIndex(s => s.kind == def.kind);

                if (slotIndex != -1)
                {
                    var targetSlot = availableSlots[slotIndex];
                    availableSlots.RemoveAt(slotIndex); // 슬롯 사용 처리 (중복 방지)

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