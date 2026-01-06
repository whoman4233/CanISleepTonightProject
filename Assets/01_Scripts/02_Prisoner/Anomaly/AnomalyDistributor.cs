using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    public static AnomalyDistributor Instance; // 외부 접근 편의용

    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonerScheduleManager scheduleManager;

    [Header("Distribution Settings")]
    [SerializeField] private int commonCount = 2;
    [SerializeField] private int individualCount = 1;
    [SerializeField] private int specialCount = 1;

    // 오늘 등장 가능한 이상현상 후보군 (테마 필터링 완료된 목록)
    private List<AnomalyDefinitionSO> currentDayPool = new List<AnomalyDefinitionSO>();

    private void Awake()
    {
        Instance = this;
        // 싱글톤이 있다면 자동 할당 시도
        if (scheduleManager == null) scheduleManager = PrisonerScheduleManager.Instance;
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
    }

    // 1단계: 전략 패턴(DailyStrategy)에서 호출하여 "오늘의 후보군"을 추립니다.
    public void FilterAnomalies(MissionDayTheme dayTheme)
    {
        currentDayPool.Clear();

        if (masterDatabase == null || masterDatabase.defs == null) return;

        foreach (var anomaly in masterDatabase.defs)
        {
            // 비트 연산: (이상현상 속성 & 오늘 테마)가 겹치면 후보 등록
            if ((anomaly.validThemes & dayTheme) != 0)
            {
                currentDayPool.Add(anomaly);
            }
        }

        Debug.Log($"[AnomalyDistributor] 테마({dayTheme}) 필터링 완료. 후보군: {currentDayPool.Count}개");
    }

    // 2단계: 필터링된 후보군(currentDayPool)을 바탕으로 각 감방에 배정합니다.
    public void DistributeAnomalies()
    {
        int currentRiotGauge = (GameManager.Instance != null) ? GameManager.Instance.CurrentRiotGauge : 0;
        var allCellIds = anchorRegistry.GetAllCellIds();

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            // 기존 배정 내역 초기화
            anchor.ClearDailyAnomalies();

            // 중복 배치 방지용 Set
            HashSet<AnomalyTargetType> usedTargets = new HashSet<AnomalyTargetType>();

            // ?? [수정됨] 리팩토링된 ScheduleManager에서 죄수 데이터 가져오기
            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);

            // 죄수가 없으면 None, 있으면 그 죄수의 성향(Trait) 가져오기
            PrisonerType pType = (pData != null) ? pData.definition.traitType : PrisonerType.None;

            // =============================================================
            // [배정 로직] MasterDB가 아닌 currentDayPool(오늘의 후보군)에서 뽑습니다.
            // =============================================================

            // 1. 특수(Special) 배정: 폭동 게이지 조건 만족하는 것만
            var cellSpecials = currentDayPool
                .Where(d => d.category == AnomalyCategory.Special && currentRiotGauge >= d.minRiotGauge)
                .ToList();
            Shuffle(cellSpecials);
            AddUniqueAnomalies(cellSpecials, anchor.currentDailyAnomalies, specialCount, usedTargets);

            // 2. 개별(Individual) 배정: 죄수 타입 일치하는 것만
            // (빈 방인 경우 pType이 None이므로 매칭되는 이상현상이 없게 됨 -> 자연스러움)
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
        }

        Debug.Log("[AnomalyDistributor] 모든 감방에 이상현상 배정 완료. (스폰은 Spawner가 담당)");
    }

    private void AddUniqueAnomalies(List<AnomalyDefinitionSO> source, List<AnomalyDefinitionSO> dest, int maxCount, HashSet<AnomalyTargetType> usedSet)
    {
        int count = 0;
        foreach (var def in source)
        {
            if (count >= maxCount) break;

            // Slot 타입(포스터 등)은 여러 개 가능, 그 외(가구 교체)는 중복 불가
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