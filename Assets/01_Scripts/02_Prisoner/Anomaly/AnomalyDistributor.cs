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
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            scheduleManager.GenerateNewResidents();
        }

        var allCellIds = anchorRegistry.GetAllCellIds();

        // 1. [덱 생성] 이번 테마의 '공용 아이템(Common)'을 모두 모아 리스트로 만듭니다.
        List<AnomalyDefinitionSO> commonDeck = new List<AnomalyDefinitionSO>();
        if (currentDayPool.Count > 0)
        {
            var commons = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
            commonDeck.AddRange(commons);

            // 2. [셔플] 리스트를 무작위로 섞습니다. (중복 방지의 핵심)
            ShuffleList(commonDeck);
        }

        // 3. 용의자 방들을 순회하며 아이템을 하나씩 나눠줍니다.
        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            anchor.ClearDailyAnomalies();

            var dailyRole = scheduleManager.GetDailyRole(cellId);

            // 용의자가 아니면 스킵
            if (!dailyRole.isSuspicious) continue;

            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            PrisonerType pType = (pData != null && pData.definition != null) ? pData.definition.traitType : PrisonerType.None;

            AnomalyDefinitionSO selectedItem = null;

            // =============================================================
            // ★ [수정] 덱에서 하나씩 꺼내주기 (Pop)
            // =============================================================

            // 전략 A: 공용 아이템(미션템)이 덱에 남아있다면 우선 배정 (1순위)
            if (commonDeck.Count > 0)
            {
                selectedItem = commonDeck[0];
                commonDeck.RemoveAt(0); // 준 건 덱에서 뺌 (중복 방지)
            }
            // 전략 B: 공용 아이템이 동났다면? -> 죄수 전용 아이템 배정 (2순위)
            else
            {
                var individualItems = currentDayPool
                    .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                    .ToList();

                if (individualItems.Count > 0)
                {
                    selectedItem = individualItems[Random.Range(0, individualItems.Count)];
                }
                // 전략 C: 그것도 없다면? -> 어쩔 수 없이 공용 풀에서 랜덤 (중복 허용)
                else
                {
                    var commonBackup = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
                    if (commonBackup.Count > 0)
                        selectedItem = commonBackup[Random.Range(0, commonBackup.Count)];
                }
            }

            // 최종 적용
            if (selectedItem != null)
            {
                anchor.currentDailyAnomalies.Add(selectedItem);
                Debug.Log($"🔴 {cellId} ({pType}) -> 범인 확정: {selectedItem.name}");
            }
            else
            {
                Debug.LogWarning($"⚠️ {cellId} ({pType}) -> 용의자지만 배정 가능한 아이템이 없음.");
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