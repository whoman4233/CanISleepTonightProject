using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// [변경] SpawnController는 이제 구조체 대신 PrisonerData 객체를 직접 받아갑니다.
// public struct PrisonerAssignment { ... } // (더 이상 사용하지 않으므로 삭제 가능)

// 하루 배정 정보를 담는 구조체 (그날의 수상함 + 그날의 기분)
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

    // [핵심 변경] ID(string)만 저장하는 게 아니라, '만들어진 죄수 데이터(PrisonerData)' 자체를 저장합니다.
    // Key: CellID, Value: PrisonerData (ID, HP, Name 등이 보존됨)
    private Dictionary<string, PrisonerData> _weeklyInstances = new Dictionary<string, PrisonerData>();

    // Schedule: '오늘 상태는 어떤가' (매일 변동)
    private List<DailyScheduleData> _weeklySchedule = new List<DailyScheduleData>();

    // [최적화] 오늘 날짜의 스케줄을 빠르게 찾기 위한 캐시
    private Dictionary<string, DailyCellAssignment> _todayCache = new Dictionary<string, DailyCellAssignment>();

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
        _onPhaseChanged = HandleGamePhaseChanged;
    }

    private void Start()
    {
        GenerateWeeklyRoster();
        GenerateWeeklySchedule();
    }

    private void OnEnable() => EventBus.Subscribe(_onPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onPhaseChanged);

    // =======================================================================
    // [핵심] SpawnController가 호출하는 메서드 (데이터 재사용)
    // =======================================================================
    public PrisonerData GetPrisonerData(string cellId)
    {
        // 1. 저장된 죄수 데이터가 있는지 확인
        if (!_weeklyInstances.TryGetValue(cellId, out PrisonerData data))
        {
            return null;
        }

        // 2. 오늘 스케줄 확인
        if (!_todayCache.TryGetValue(cellId, out DailyCellAssignment dailyInfo))
        {
            return null;
        }

        // 3. [기존] 오늘의 성향 업데이트
        data.RuntimeAIType = dailyInfo.dailyAIType;

        // 4. [신규 추가] 매일 아침 상태 리셋 (이 부분이 HP 초기화 핵심!)
        data.CurrentHealth = data.MaxHealth; // 체력 풀회복
        data.IsSuppressed = false;           // 제압 상태 해제

        return data;
    }

    // =======================================================================
    // 1. Roster Logic (데이터 생성 - 일주일에 한 번만!)
    // =======================================================================
    [ContextMenu("Generate Roster Now")]
    public void GenerateWeeklyRoster()
    {
        // [방어 코드] 이미 데이터가 있다면 다시 만들지 않음 (일주일 유지)
        if (_weeklyInstances.Count > 0)
        {
            Debug.Log("[Schedule] 이미 생성된 주간 명부가 있습니다. 생략합니다.");
            return;
        }

        _weeklyInstances.Clear();

        if (prisonerDatabase == null || prisonerDatabase.prisoners.Count == 0) return;

        var allAnchors = FindObjectsOfType<CellAnchor>();
        var allCellIds = allAnchors.Select(a => a.cellId).ToList();

        foreach (string cellId in allCellIds)
        {
            var randomDef = prisonerDatabase.GetRandomDefinition();
            if (randomDef != null)
            {
                // [여기서 생성!] new PrisonerData를 여기서 딱 한 번만 합니다.
                // 초기 성향은 Good(혹은 Normal)으로 설정 (매일 아침 바뀜)
                PrisonerData newData = new PrisonerData(randomDef, PrisonerAIType.Good);

                // 생성된 데이터를 딕셔너리에 보관 -> 일주일 내내 꺼내 씀
                _weeklyInstances[cellId] = newData;
            }
        }
        Debug.Log($"[Schedule] Roster Generated for {allCellIds.Count} cells (Fixed for Week).");
    }

    // =======================================================================
    // 2. Schedule Logic
    // =======================================================================
    [ContextMenu("Generate Schedule Now")]
    public void GenerateWeeklySchedule()
    {
        _weeklySchedule.Clear();

        // [변경] _weeklyInstances 키를 기반으로 스케줄 생성
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

                    // [유지] 작성하신 Enum 값 (Good/Bad) 사용
                    PrisonerAIType dailyMood = (UnityEngine.Random.value > 0.5f)
                                               ? PrisonerAIType.Good
                                               : PrisonerAIType.Bad;

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
        Debug.Log($"[Schedule] Generated Daily Schedules.");
    }

    // 오늘 스케줄(Active 목록) 가져오기
    public Dictionary<string, bool> GetScheduleForDay(int dayNumber)
    {
        var result = new Dictionary<string, bool>();
        var data = _weeklySchedule.Find(x => x.dayNumber == dayNumber);

        if (data != null)
        {
            foreach (var kvp in data.cellAssignments)
            {
                result.Add(kvp.Key, kvp.Value.isSuspicious);
            }
        }
        return result;
    }

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.Phase == GamePhase.Briefing && GameManager.Instance != null)
        {
            int currentDay = GameManager.Instance.CurrentDay;

            // 1. 오늘의 스케줄 데이터를 찾아서 캐싱
            _todayCache.Clear();
            var daySchedule = _weeklySchedule.Find(x => x.dayNumber == currentDay);
            if (daySchedule != null)
            {
                _todayCache = daySchedule.cellAssignments;
            }
        }
    }

    // [추가] AnomalyDistributor가 참조하는 메서드 (데이터 기반으로 변경)
    public PrisonerDefinition GetAssignedPrisonerDef(string cellId)
    {
        if (_weeklyInstances.TryGetValue(cellId, out PrisonerData data))
        {
            return data.definition;
        }
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