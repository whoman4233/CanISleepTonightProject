using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Log 출력을 위해 Linq 사용 (Select, String.Join)

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
    [SerializeField] private int specialCount = 1;

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
            if ((anomaly.validThemes & dayTheme) != 0)
            {
                currentDayPool.Add(anomaly);
            }
        }
        Debug.Log($"[AnomalyDistributor] 테마({dayTheme}) 필터링 완료. 후보군: {currentDayPool.Count}개");
    }

    public void DistributeAnomalies()
    {
        var allCellIds = anchorRegistry.GetAllCellIds();

        Debug.Log("========== [AnomalyDistributor] 이상현상 배정 시작 ==========");

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            anchor.ClearDailyAnomalies();
            HashSet<AnomalyTargetType> usedTargets = new HashSet<AnomalyTargetType>();

            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            var dailyRole = scheduleManager.GetDailyRole(cellId);

            PrisonerType pType = (pData != null) ? pData.definition.traitType : PrisonerType.None;

            // =============================================================
            // 0. 미션 타겟(용의자) 강제 배정 (구조체 null 체크 오류 수정됨)
            // =============================================================
            if (dailyRole.isSuspicious)
            {
                var missionCandidates = currentDayPool
                    .Where(d => d.category == AnomalyCategory.Individual)
                    .ToList();

                if (missionCandidates.Count == 0) missionCandidates = currentDayPool;

                if (missionCandidates.Count > 0)
                {
                    Shuffle(missionCandidates);
                    var missionAnomaly = missionCandidates[0];

                    anchor.currentDailyAnomalies.Add(missionAnomaly);
                    usedTargets.Add(missionAnomaly.targetType);
                }
            }

            // 2. 개별(Individual) 배정
            var cellIndividual = currentDayPool
                .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                .ToList();
            Shuffle(cellIndividual);
            AddUniqueAnomalies(cellIndividual, anchor.currentDailyAnomalies, individualCount, usedTargets);

            // 3. 공통(Common) 배정
            var cellCommons = currentDayPool
                .Where(d => d.category == AnomalyCategory.Common)
                .ToList();
            Shuffle(cellCommons);
            AddUniqueAnomalies(cellCommons, anchor.currentDailyAnomalies, commonCount, usedTargets);

            // =============================================================
            // ★ [추가] 배정 결과 로그 출력
            // =============================================================
            if (anchor.currentDailyAnomalies.Count > 0)
            {
                // 리스트에 있는 이상현상 이름들을 쉼표로 연결해서 출력
                string assignedNames = string.Join(", ", anchor.currentDailyAnomalies.Select(a => a.name));
                Debug.Log($"[AnomalyDistributor] 방 {cellId} 배정됨 ({anchor.currentDailyAnomalies.Count}개): [{assignedNames}]");
            }
            else
            {
                // 이상현상이 하나도 없는 경우 (필요하다면 주석 해제)
                Debug.Log($"[AnomalyDistributor] 방 {cellId} 배정 없음");
            }
        }

        Debug.Log("========== [AnomalyDistributor] 이상현상 배정 완료 ==========");
    }

    private void AddUniqueAnomalies(List<AnomalyDefinitionSO> source, List<AnomalyDefinitionSO> dest, int maxCount, HashSet<AnomalyTargetType> usedSet)
    {
        int count = 0;
        foreach (var def in source)
        {
            if (count >= maxCount) break;

            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (usedSet.Contains(def.targetType)) continue;
                usedSet.Add(def.targetType);
            }

            dest.Add(def);
            count++;
        }
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