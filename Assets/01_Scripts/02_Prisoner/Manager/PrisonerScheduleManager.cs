using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// [필수] SpawnController와 통신하기 위한 구조체
public struct PrisonerAssignment
{
    public string templateId; // Roster에서 가져옴 (누구인가)
    public PrisonerAIType aiType; // Schedule에서 가져옴 (오늘 기분이 어떤가)
}

// [신규] 하루 배정 정보를 담는 구조체 (그날의 수상함 + 그날의 기분)
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

    // Roster: '누구인가(TemplateId)' (불변)
    private Dictionary<string, string> _weeklyRoster = new Dictionary<string, string>();

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
    // [핵심] SpawnController가 호출하는 메서드 (데이터 병합)
    // =======================================================================
    public PrisonerAssignment? GetAssignment(string cellId)
    {
        // 1. 이 방에 죄수가 배정되어 있는가? (Roster 확인)
        if (!_weeklyRoster.TryGetValue(cellId, out string tid))
        {
            return null; // 아무도 안 사는 방
        }

        // 2. 이 방이 오늘 활성화(출근/감방체류) 상태인가? (Cache 확인)
        if (!_todayCache.TryGetValue(cellId, out DailyCellAssignment dailyData))
        {
            return null; // 오늘은 비번이거나 빈 방 취급
        }

        // 3. 두 정보를 합쳐서 반환
        return new PrisonerAssignment
        {
            templateId = tid,
            aiType = dailyData.dailyAIType
        };
    }

    // =======================================================================
    // 1. Roster Logic
    // =======================================================================
    [ContextMenu("Generate Roster Now")]
    public void GenerateWeeklyRoster()
    {
        _weeklyRoster.Clear();

        if (prisonerDatabase == null || prisonerDatabase.prisoners.Count == 0) return;

        var allAnchors = FindObjectsOfType<CellAnchor>();
        var allCellIds = allAnchors.Select(a => a.cellId).ToList();

        foreach (string cellId in allCellIds)
        {
            var randomDef = prisonerDatabase.GetRandomDefinition();
            if (randomDef != null)
            {
                _weeklyRoster[cellId] = randomDef.templateId;
            }
        }
        Debug.Log($"[Schedule] Roster Generated for {allCellIds.Count} cells.");
    }

    // =======================================================================
    // 2. Schedule Logic
    // =======================================================================
    [ContextMenu("Generate Schedule Now")]
    public void GenerateWeeklySchedule()
    {
        _weeklySchedule.Clear();
        var allCellIds = _weeklyRoster.Keys.ToList();

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

                    // [수정] Enum 값은 본인의 프로젝트 정의에 맞게 수정하세요 (예: Normal, Aggressive)
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
                // Key: CellID, Value: IsSuspicious
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

            // 1. 오늘의 스케줄 데이터를 찾아서 캐싱 (GetAssignment 최적화용)
            _todayCache.Clear();
            var daySchedule = _weeklySchedule.Find(x => x.dayNumber == currentDay);
            if (daySchedule != null)
            {
                _todayCache = daySchedule.cellAssignments;
            }

            // 2. PrisonManager 등 외부 시스템에 알림
            // (필요 시 구현)
            // prisonManager.ApplyDailySchedule(_todayCache); 
        }
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

    public PrisonerDefinition GetAssignedPrisonerDef(string cellId)
    {
        // 1. Roster(명부)에서 해당 방에 누가 사는지 ID 확인
        if (_weeklyRoster.TryGetValue(cellId, out string templateId))
        {
            // 2. 데이터베이스에서 ID로 실제 데이터(SO)를 찾아서 반환
            if (prisonerDatabase.TryGet(templateId, out var def))
            {
                return def;
            }
        }

        // 해당 방에 배정된 죄수가 없거나, DB에 데이터가 없으면 null 반환
        return null;
    }
}