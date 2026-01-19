using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text; // 로그용

public class AnomalyDistributor : MonoBehaviour
{
    public static AnomalyDistributor Instance;

    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonerScheduleManager scheduleManager;

    [Header("Distribution Settings")]
    [SerializeField] private int commonCount = 2;
    [SerializeField] private int individualCount = 1;

    [Header("Debug")]
    [SerializeField] private bool showDetailedLog = true; // 켜두면 상세 로그 나옴

    private List<AnomalyDefinitionSO> currentDayPool = new List<AnomalyDefinitionSO>();

    private void Awake()
    {
        Instance = this;
        if (scheduleManager == null) scheduleManager = PrisonerScheduleManager.Instance;
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
    }

    public void FilterAnomalies(MissionDayTheme dayTheme)
    {
        currentDayPool.Clear();
        if (masterDatabase == null || masterDatabase.defs == null) return;

        foreach (var anomaly in masterDatabase.defs)
        {
            if (anomaly == null) continue;
            // 테마 필터링 (일단 다 담고 나중에 거름)
            if ((anomaly.validThemes & dayTheme) != 0)
            {
                currentDayPool.Add(anomaly);
            }
        }
    }

    public void DistributeAnomalies()
    {
        // 1. 죄수 데이터 확인
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            Debug.LogWarning(" [AnomalyDistributor] 죄수 데이터 없음 -> 강제 생성 시도");
            scheduleManager.GenerateNewResidents();
        }

        var allCellIds = anchorRegistry.GetAllCellIds();
        Debug.Log($"[AnomalyDistributor] 총 {allCellIds.Count}개의 방에 배정 시작 (Pool: {currentDayPool.Count}개)");

        if (currentDayPool.Count == 0)
        {
            Debug.LogError("🚨 [Error] 풀이 비어있습니다! FilterAnomalies가 호출되지 않았거나 DB 설정 문제.");
            FilterAnomalies((MissionDayTheme)~0); // 비상 복구
        }

        int activeRoomCount = 0;

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            anchor.ClearDailyAnomalies();
            HashSet<AnomalyTargetType> usedTargets = new HashSet<AnomalyTargetType>();

            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            var dailyRole = scheduleManager.GetDailyRole(cellId);
            PrisonerType pType = (pData != null && pData.definition != null) ? pData.definition.traitType : PrisonerType.None;

            activeRoomCount++;
            StringBuilder roomLog = new StringBuilder();
            roomLog.Append($"[{cellId} / {pType}] ");

            // =============================================================
            // 0. 미션 타겟(범인) 배정 - ★ [수정] 타입 일치 필수!
            // =============================================================
            if (dailyRole.isSuspicious)
            {
                // 수정: 범인을 뽑을 때도 '죄수 타입'이 맞는 것 중에서만 뽑는다.
                var missionCandidates = currentDayPool
                    .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                    .ToList();

                // 만약 타입 맞는 게 없으면 공통(Common) 중에서라도 뽑는다.
                if (missionCandidates.Count == 0)
                {
                    missionCandidates = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
                    roomLog.Append("<Color=red>[범인후보없음->공통대체]</color> ");
                }

                if (missionCandidates.Count > 0)
                {
                    Shuffle(missionCandidates);
                    var missionAnomaly = missionCandidates[0];

                    anchor.currentDailyAnomalies.Add(missionAnomaly);
                    usedTargets.Add(missionAnomaly.targetType);
                    roomLog.Append($"<Color=yellow>범인:{missionAnomaly.name}</color> | ");
                }
                else
                {
                    roomLog.Append("<Color=red>[범인배정실패-DB확인필요]</color> | ");
                }
            }

            // =============================================================
            // 2. 개별(Individual) 배정
            // =============================================================
            // 로직: Always가 아닌 순수 랜덤템 우선, 없으면 Always라도 가져와서 채움
            var validIndividuals = currentDayPool
                .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                .ToList();

            // 1순위: Always 아닌 애들 (진짜 랜덤)
            var candidatesIndiv = validIndividuals.Where(d => !d.alwaysSpawnNormal).ToList();

            // 만약 진짜 랜덤이 모자라면? Always 켜진 애들도 후보에 포함 (빈방 방지)
            if (candidatesIndiv.Count < individualCount)
            {
                candidatesIndiv = validIndividuals;
            }

            Shuffle(candidatesIndiv);
            int addedIndiv = AddUniqueAnomalies(candidatesIndiv, anchor.currentDailyAnomalies, individualCount, usedTargets);
            roomLog.Append($"개별:{addedIndiv}개 ");

            // =============================================================
            // 3. 공통(Common) 배정
            // =============================================================
            var validCommons = currentDayPool
                .Where(d => d.category == AnomalyCategory.Common)
                .ToList();

            var candidatesCommon = validCommons.Where(d => !d.alwaysSpawnNormal).ToList();

            // 역시 모자라면 Always 포함
            if (candidatesCommon.Count < commonCount)
            {
                candidatesCommon = validCommons;
            }

            Shuffle(candidatesCommon);
            int addedCommon = AddUniqueAnomalies(candidatesCommon, anchor.currentDailyAnomalies, commonCount, usedTargets);
            roomLog.Append($"공통:{addedCommon}개 ");

            // =============================================================
            // 로그 출력
            // =============================================================
            if (showDetailedLog)
            {
                string assignedList = string.Join(", ", anchor.currentDailyAnomalies.Select(a => a.name));

                // 하나도 배정 안 됐으면 경고 로그
                if (anchor.currentDailyAnomalies.Count == 0)
                {
                    Debug.LogError($"{roomLog} -> 배정된 이상현상 0개! (후보군 부족: Indiv후보 {candidatesIndiv.Count}개 / Common후보 {candidatesCommon.Count}개)");
                }
                else
                {
                    Debug.Log($"{roomLog} -> [{assignedList}]");
                }
            }
        }

        Debug.Log($" [AnomalyDistributor] 배정 종료. (총 활성 방: {activeRoomCount} / 12 예상)");
    }

    private int AddUniqueAnomalies(List<AnomalyDefinitionSO> source, List<AnomalyDefinitionSO> dest, int maxCount, HashSet<AnomalyTargetType> usedSet)
    {
        int count = 0;
        foreach (var def in source)
        {
            if (count >= maxCount) break;

            // 이미 범인 등으로 선정된 타겟타입(슬롯 아님)이면 스킵
            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (usedSet.Contains(def.targetType)) continue;
                usedSet.Add(def.targetType);
            }

            dest.Add(def);
            count++;
        }
        return count;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int rnd = Random.Range(i, list.Count);
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}