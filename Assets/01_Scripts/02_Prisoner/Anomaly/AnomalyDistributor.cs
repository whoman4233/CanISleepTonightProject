using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    public static AnomalyDistributor Instance;

    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonerScheduleManager scheduleManager;

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
            if ((anomaly.validThemes & dayTheme) != 0)
            {
                currentDayPool.Add(anomaly);
            }
        }
        Debug.Log($"[AnomalyDistributor] 테마({dayTheme}) 필터링 결과: {currentDayPool.Count}개 후보 등록됨.");
    }

    public void DistributeAnomalies()
    {
        // 1. 거주민 생성 체크
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            scheduleManager.GenerateNewResidents();
        }

        var allCellIds = anchorRegistry.GetAllCellIds();

        // 2. [공용 덱 생성] 이번 테마의 'Common' 아이템만 모음
        List<AnomalyDefinitionSO> commonDeck = new List<AnomalyDefinitionSO>();

        // 덱 리필 함수
        void RefillDeck()
        {
            if (currentDayPool.Count > 0)
            {
                var commons = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
                if (commons.Count > 0)
                {
                    commonDeck.AddRange(commons);
                    ShuffleList(commonDeck);
                }
            }
        }

        RefillDeck(); // 초기 1회 리필

        // 3. 방 순회 및 배정
        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;
            anchor.ClearDailyAnomalies();

            var dailyRole = scheduleManager.GetDailyRole(cellId);
            if (!dailyRole.isSuspicious) continue; // 용의자 아니면 패스

            // 죄수 정보 가져오기
            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            PrisonerType pType = (pData != null && pData.definition != null) ? pData.definition.traitType : PrisonerType.None;

            AnomalyDefinitionSO selectedItem = null;

            // =============================================================
            // ★ [수정] 1순위: 죄수 전용 아이템(Individual) 확인
            // =============================================================
            var mySpecialItems = currentDayPool
                .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                .ToList();

            // 전용 아이템이 존재한다면, 70% 확률로 전용 아이템 배정 (확률은 조절 가능)
            if (mySpecialItems.Count > 0 && Random.value < 0.7f)
            {
                selectedItem = mySpecialItems[Random.Range(0, mySpecialItems.Count)];
                // Debug.Log($"[Anomaly] {cellId}({pType}) -> 전용 아이템 당첨: {selectedItem.name}");
            }
            // =============================================================
            // ★ [수정] 2순위: 공용 아이템(Common) 배정
            // =============================================================
            else
            {
                if (commonDeck.Count == 0) RefillDeck(); // 부족하면 리필

                if (commonDeck.Count > 0)
                {
                    selectedItem = commonDeck[0];
                    commonDeck.RemoveAt(0); // 중복 방지 (덱에서 제거)
                }
            }

            // 최종 적용
            if (selectedItem != null)
            {
                anchor.currentDailyAnomalies.Add(selectedItem);
            }
            else
            {
                Debug.LogWarning($"[Anomaly] {cellId}에 배정할 아이템이 없습니다. (Pool 확인 필요)");
            }
        }
    }

    // 리스트 섞기 함수 (Fisher-Yates Shuffle)
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int rnd = Random.Range(i, list.Count);
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    public void ForceAddAnomaly(string cellId, AnomalyDefinitionSO itemDef)
    {
        if (anchorRegistry.TryGet(cellId, out var anchor))
        {
            anchor.ClearDailyAnomalies();
            anchor.currentDailyAnomalies.Add(itemDef);
            Debug.Log($"[Mission] {cellId}에 미션 아이템 강제 배정: {itemDef.name}");
        }
    }
}