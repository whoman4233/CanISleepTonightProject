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
        // 1. 거주민 생성 체크 (안전장치)
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            scheduleManager.GenerateNewResidents();
        }

        var allCellIds = anchorRegistry.GetAllCellIds();

        // 2. [초기 덱 생성] 이번 테마의 '공용 아이템(Common)'을 모두 모아 리스트로 만듭니다.
        List<AnomalyDefinitionSO> commonDeck = new List<AnomalyDefinitionSO>();

        // 헬퍼 로직: 덱 리필 (중복 허용을 위해 다시 채워넣고 섞음)
        void RefillDeck()
        {
            if (currentDayPool.Count > 0)
            {
                // 이번 테마에 맞는 Common(미션) 아이템만 추려서 덱에 추가
                var commons = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
                if (commons.Count > 0)
                {
                    commonDeck.AddRange(commons);
                    ShuffleList(commonDeck);
                    // Debug.Log($"[Anomaly] 덱이 리필되었습니다. 현재 잔여량: {commonDeck.Count}");
                }
            }
        }

        // 처음에 한 번 덱 채우기
        RefillDeck();

        // 3. [방 기준 순회] 모든 방을 돌며 용의자(Sus)를 찾습니다.
        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            // 방의 기존 아이템 초기화
            anchor.ClearDailyAnomalies();

            var dailyRole = scheduleManager.GetDailyRole(cellId);

            // 용의자(Suspicious)가 아니면 아이템을 주지 않고 넘어갑니다.
            if (!dailyRole.isSuspicious) continue;

            AnomalyDefinitionSO selectedItem = null;

            // =============================================================
            // ★ [수정] "방 당 1개 보장" 로직 (부족하면 리필)
            // =============================================================

            // 1. 줄 아이템이 없으면 리필합니다. (아이템 데이터가 적어도 문제 없음)
            if (commonDeck.Count == 0)
            {
                RefillDeck();
            }

            // 2. 덱에서 하나 꺼내서 방에 배정합니다.
            if (commonDeck.Count > 0)
            {
                selectedItem = commonDeck[0];
                commonDeck.RemoveAt(0); // 중복 방지를 위해 덱에서 제거
            }
            else
            {
                // 리필을 시도했는데도 덱이 비어있다면, SO 데이터가 아예 없는 경우입니다.
                Debug.LogError($"[Anomaly] '{cellId}' 방에 줄 아이템이 없습니다! (테마 아이템 데이터 확인 필요)");
                continue;
            }

            // 3. 최종 적용
            if (selectedItem != null)
            {
                anchor.currentDailyAnomalies.Add(selectedItem);
                Debug.Log($"🔴 {cellId} (용의자) -> 아이템 배정 완료: {selectedItem.name}");
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