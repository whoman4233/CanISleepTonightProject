using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 하루 배정 정보를 담는 구조체
[System.Serializable]
public struct DailyCellAssignment
{
    public bool isSuspicious;
    public PrisonerAIType dailyAIType;
}

[System.Serializable]
public class DailyScheduleData
{
    public int dayNumber;
    public Dictionary<string, DailyCellAssignment> cellAssignments = new Dictionary<string, DailyCellAssignment>();
}

public class PrisonerScheduleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;

    [Header("Settings")]
    [SerializeField] private int daysInWeek = 7;
    [SerializeField] private int dailyActiveCount = 6;
    [SerializeField] private int dailySuspiciousCount = 3;

    // 데이터 저장소
    private Dictionary<string, PrisonerData> _weeklyInstances = new Dictionary<string, PrisonerData>();
    private List<DailyScheduleData> _weeklySchedule = new List<DailyScheduleData>();
    private Dictionary<string, DailyCellAssignment> _todayCache = new Dictionary<string, DailyCellAssignment>();

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
        _onPhaseChanged = HandleGamePhaseChanged;

        // 데이터 미리 생성
        GenerateWeeklyRoster();
        GenerateWeeklySchedule();
    }

    private void OnEnable() => EventBus.Subscribe(_onPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onPhaseChanged);

    // =======================================================================
    // [핵심 수정] PrisonManager가 호출할 때 캐시도 같이 갱신합니다!
    // =======================================================================
    public Dictionary<string, DailyCellAssignment> GetAssignmentsForDay(int dayNumber)
    {
        if (dayNumber <= 0) dayNumber = 1;

        var dayData = _weeklySchedule.Find(x => x.dayNumber == dayNumber);

        if (dayData != null)
        {
            // 🔥 [수정] 데이터를 내보내면서 "오늘의 캐시"로 등록합니다.
            // 이렇게 하면 Standby 페이즈에서도 캐시가 즉시 채워집니다.
            _todayCache = dayData.cellAssignments;

            return dayData.cellAssignments;
        }

        Debug.LogError($"[ScheduleManager] {dayNumber}일차 스케줄 데이터가 없습니다!");
        return new Dictionary<string, DailyCellAssignment>();
    }

    // =======================================================================
    // [핵심 수정] 캐시가 없어도 죽지 않도록 안전장치 추가
    // =======================================================================
    public PrisonerData GetPrisonerData(string cellId)
    {
        if (!_weeklyInstances.TryGetValue(cellId, out PrisonerData data)) return null;

        // 1. 오늘 캐시 확인
        if (_todayCache.TryGetValue(cellId, out DailyCellAssignment dailyInfo))
        {
            data.RuntimeAIType = dailyInfo.dailyAIType;
        }
        else
        {
            // 🔥 [수정] 캐시가 없다고 null 리턴하면 안 됩니다! (데이터는 있으니까요)
            // 대신 기본값이나 기존 값을 유지한 채 리턴합니다.
            // Debug.LogWarning($"[Schedule] {cellId}의 오늘 스케줄 캐시가 없습니다. 기본 AI로 진행합니다.");
        }

        // 2. 상태 초기화 (HP 회복 등)
        data.CurrentHealth = data.MaxHealth;
        data.IsSuppressed = false;

        return data;
    }

    // ... (이하 Generate 함수 등은 기존과 동일) ...

    [ContextMenu("Generate Roster Now")]
    public void GenerateWeeklyRoster()
    {
        if (_weeklyInstances.Count > 0) return;

        _weeklyInstances.Clear();
        if (prisonerDatabase == null || prisonerDatabase.prisoners.Count == 0) return;

        var allAnchors = FindObjectsOfType<CellAnchor>();
        var allCellIds = allAnchors.Select(a => a.cellId).ToList();

        foreach (string cellId in allCellIds)
        {
            var randomDef = prisonerDatabase.GetRandomDefinition();
            if (randomDef != null)
            {
                PrisonerData newData = new PrisonerData(randomDef, PrisonerAIType.Good);
                _weeklyInstances[cellId] = newData;
            }
        }
        Debug.Log($"[Schedule] {allCellIds.Count}개 감방 명부 작성 완료.");
    }

    [ContextMenu("Generate Schedule Now")]
    public void GenerateWeeklySchedule()
    {
        _weeklySchedule.Clear();
        var allCellIds = _weeklyInstances.Keys.ToList();
        if (allCellIds.Count == 0) return;

        for (int day = 1; day <= daysInWeek; day++)
        {
            DailyScheduleData dayData = new DailyScheduleData();
            dayData.dayNumber = day;
            Shuffle(allCellIds);

            for (int i = 0; i < allCellIds.Count; i++)
            {
                string id = allCellIds[i];
                bool isActive = i < dailyActiveCount;

                if (isActive)
                {
                    bool isSuspicious = (i < dailySuspiciousCount);
                    PrisonerAIType dailyMood = (UnityEngine.Random.value > 0.5f) ? PrisonerAIType.Good : PrisonerAIType.Bad;

                    DailyCellAssignment assignment = new DailyCellAssignment
                    {
                        isSuspicious = isSuspicious,
                        dailyAIType = dailyMood
                    };
                    dayData.cellAssignments.Add(id, assignment);
                }
            }
            _weeklySchedule.Add(dayData);
        }
        Debug.Log($"[Schedule] 1~{daysInWeek}일차 스케줄 생성 완료.");
    }

    public Dictionary<string, bool> GetScheduleForDay(int dayNumber)
    {
        var result = new Dictionary<string, bool>();
        var assignments = GetAssignmentsForDay(dayNumber);
        foreach (var kvp in assignments)
        {
            result.Add(kvp.Key, kvp.Value.isSuspicious);
        }
        return result;
    }

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        // Standby에서 이미 캐시가 업데이트되었으므로 여기는 안전장치 역할만 합니다.
        if (evt.Phase == GamePhase.Briefing && GameManager.Instance != null)
        {
            // 혹시 모르니 한 번 더 확인
            if (_todayCache.Count == 0)
            {
                int currentDay = GameManager.Instance.CurrentDay;
                GetAssignmentsForDay(currentDay);
            }
        }
    }

    public PrisonerDefinition GetAssignedPrisonerDef(string cellId)
    {
        if (_weeklyInstances.TryGetValue(cellId, out PrisonerData data))
            return data.definition;
        return null;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int rnd = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}