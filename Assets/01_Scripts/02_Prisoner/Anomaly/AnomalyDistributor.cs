using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    public static AnomalyDistributor Instance;

    [Header("Debug")]
    public bool enableDebugLogs = true; // 로그 온오프용 변수 추가

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

        if (enableDebugLogs)
            Debug.Log($"[AnomalyDistributor] 테마({dayTheme}) 필터링 결과: {currentDayPool.Count}개 후보 등록됨.");
    }

    public void DistributeAnomalies()
    {
        // 1. 거주민 생성 체크
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[Anomaly] 거주민이 없어서 새로 생성합니다.");
            scheduleManager.GenerateNewResidents();
        }

        // 2. 미션 테마 확인
        MissionDayTheme currentTheme = MissionDayTheme.All;
        if (DailyMissionManager.Instance != null && DailyMissionManager.Instance.CurrentMission != null)
        {
            currentTheme = DailyMissionManager.Instance.CurrentMission.missionTheme;
        }

        if (enableDebugLogs)
            Debug.Log($"<color=cyan>[Anomaly] 오늘의 미션 테마: {currentTheme} (Day {DailyMissionManager.Instance?.CurrentMission?.missionId})</color>");

        // 3. 오늘의 풀(Pool) 필터링 상태 확인 (핵심 로그)
        // currentDayPool은 StartDay 등에서 미리 채워져 있어야 함. 확인 차 여기서 다시 필터링 로직 체크
        int commonCount = currentDayPool.Count(x => x.category == AnomalyCategory.Common);
        int indivCount = currentDayPool.Count(x => x.category == AnomalyCategory.Individual);

        if (enableDebugLogs)
            Debug.Log($"[Anomaly] 현재 풀 상태 -> 공용: {commonCount}개, 전용: {indivCount}개");

        // 만약 풀이 비어있다면 데이터 설정 문제임
        if (currentDayPool.Count == 0)
        {
            if (enableDebugLogs)
                Debug.Log($"<color=red>[Fatal] {currentTheme} 테마에 해당하는 아이템이 하나도 없습니다! 아이템 데이터(SO)의 Valid Theme를 확인하세요.</color>");
            return;
        }

        // 4. 공용 덱 구성
        List<AnomalyDefinitionSO> commonDeck = new List<AnomalyDefinitionSO>();
        void RefillDeck()
        {
            var commons = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
            if (commons.Count > 0)
            {
                commonDeck.AddRange(commons);
                ShuffleList(commonDeck);
                if (enableDebugLogs)
                    Debug.Log($"[Anomaly] 공용 덱 리필됨 ({commons.Count}개)");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[Anomaly] 리필할 공용(Common) 아이템이 없습니다. (테마: {currentTheme})");
            }
        }
        RefillDeck();

        // 5. 배정 시작
        var allCellIds = anchorRegistry.GetAllCellIds();
        int assignedCount = 0;

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;
            anchor.ClearDailyAnomalies();

            var dailyRole = scheduleManager.GetDailyRole(cellId);

            // 용의자가 아니면 배정 안 함 (기획 의도에 따라 변경 가능)
            if (!dailyRole.isSuspicious)
            {
                continue;
            }

            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            PrisonerType pType = (pData != null && pData.definition != null) ? pData.definition.traitType : PrisonerType.None;

            AnomalyDefinitionSO selectedItem = null;
            string assignSource = "";

            // (A) 죄수 전용 아이템(Individual) 우선 확인
            var mySpecialItems = currentDayPool
                .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                .ToList();

            if (mySpecialItems.Count > 0 && Random.value < 0.6f) // 60% 확률로 전용템
            {
                selectedItem = mySpecialItems[Random.Range(0, mySpecialItems.Count)];
                assignSource = $"전용({pType})";
            }
            // (B) 공용 아이템(Common) 배정
            else
            {
                if (commonDeck.Count == 0) RefillDeck();

                if (commonDeck.Count > 0)
                {
                    selectedItem = commonDeck[0];
                    commonDeck.RemoveAt(0);
                    assignSource = "공용";
                }
            }

            if (selectedItem != null)
            {
                anchor.currentDailyAnomalies.Add(selectedItem);
                if (enableDebugLogs)
                    Debug.Log($"[Anomaly] {cellId} ({pType}) -> <color=yellow>{selectedItem.name}</color> 배정완료 ({assignSource})");
                assignedCount++;
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogError($"[Anomaly] {cellId} ({pType}) -> 배정 실패! (줄 아이템이 없음)");
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[Anomaly] 총 {assignedCount}개의 이상현상 아이템이 배정되었습니다.");
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
            if (enableDebugLogs)
                Debug.Log($"[Mission] {cellId}에 미션 아이템 강제 배정: {itemDef.name}");
        }
    }
}