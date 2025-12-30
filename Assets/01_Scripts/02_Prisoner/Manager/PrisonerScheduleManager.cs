using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DailyScheduleData
{
    public int dayNumber;
    // Key: CellId, Value: IsSuspicious (true면 이상현상 발생, false면 정상)
    // 필요하다면 Value를 구조체로 만들어 죄수 ID 등을 포함시킬 수 있음
    public Dictionary<string, bool> cellAssignments = new Dictionary<string, bool>();
}

public class PrisonerScheduleManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int daysInWeek = 7;
    [SerializeField] private PrisonCellManager cellManager;

    // 생성된 스케줄 저장소
    private List<DailyScheduleData> _weeklySchedule = new List<DailyScheduleData>();

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();

        GenerateWeeklySchedule(); // 1일차~7일차 생성

        // 이벤트 구독 설정
        _onPhaseChanged = HandleGamePhaseChanged;
    }

    private void OnEnable() => EventBus.Subscribe(_onPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onPhaseChanged);

    /// <summary>
    /// 일주일치 스케줄을 미리 랜덤 생성하여 저장
    /// </summary>
    public void GenerateWeeklySchedule()
    {
        _weeklySchedule.Clear();
        var allCellIds = new List<string>();

        // CellManager가 초기화된 상태여야 함
        foreach (var cell in cellManager.Cells)
            allCellIds.Add(cell.CellId);

        for (int day = 1; day <= daysInWeek; day++)
        {
            DailyScheduleData dayData = new DailyScheduleData();
            dayData.dayNumber = day;

            // --- [설정 로직] 날짜별 활성/수상함 개수 설정 ---
            int activeCount = 6; // 예: 매일 6개 활성화
            int suspiciousCount = 3; // 예: 그 중 3개는 수상함
            // ---------------------------------------------

            // 1. 방 섞기
            Shuffle(allCellIds);

            // 2. 활성 방 선택
            for (int i = 0; i < allCellIds.Count; i++)
            {
                string id = allCellIds[i];
                bool isActive = i < activeCount;
                bool isSuspicious = false;

                if (isActive)
                {
                    // 활성 방 중 앞쪽 N개는 수상한 방으로 설정
                    if (i < suspiciousCount) isSuspicious = true;
                }

                // 활성화된 방만 기록 (또는 전체 기록)
                if (isActive)
                {
                    dayData.cellAssignments.Add(id, isSuspicious);
                }
            }

            _weeklySchedule.Add(dayData);
        }

        Debug.Log($"[Schedule] Generated schedule for {daysInWeek} days.");
    }

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        // Standby 페이즈가 되면 매니저에게 오늘 할당량을 전달
        if (evt.Phase == GamePhase.Standby)
        {
            int currentDay = GameManager.Instance.CurrentDay; // 현재 날짜
            var todaysPlan = GetScheduleForDay(currentDay);

            if (todaysPlan.Count > 0)
            {
                cellManager.ApplyDailySchedule(todaysPlan);
            }
            else
            {
                Debug.LogWarning($"[Schedule] No plan for Day {currentDay}. Running fallback random setup.");
            }
        }
    }

    /// <summary>
    /// 특정 날짜의 배정 데이터를 가져옴
    /// </summary>
    public Dictionary<string, bool> GetScheduleForDay(int dayNumber)
    {
        var data = _weeklySchedule.Find(x => x.dayNumber == dayNumber);
        if (data != null) return data.cellAssignments;

        Debug.LogWarning($"No schedule found for Day {dayNumber}");
        return new Dictionary<string, bool>();
    }

    // 유틸리티: 리스트 섞기
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}