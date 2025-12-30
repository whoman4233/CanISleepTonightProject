using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// [신규] 하루 배정 정보를 담는 구조체 (그날의 수상함 + 그날의 기분)
[System.Serializable]
public struct DailyCellAssignment
{
    public bool isSuspicious;
    public PrisonerAIType dailyAIType; // 매일 바뀌는 성향
}

[System.Serializable]
public class DailyScheduleData
{
    public int dayNumber;
    // [변경] Key: CellId, Value: 상세 배정 정보 (기존 bool -> 구조체)
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

    // [변경] Roster는 이제 '누구인가(TemplateId)'만 기억합니다. (성향 삭제)
    private Dictionary<string, string> _weeklyRoster = new Dictionary<string, string>();

    // 스케줄 데이터
    private List<DailyScheduleData> _weeklySchedule = new List<DailyScheduleData>();

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

    // -----------------------------------------------------------------------
    // 1. Roster Logic (누가 어디 사나? - 불변)
    // -----------------------------------------------------------------------
    [ContextMenu("Generate Roster Now")]
    public void GenerateWeeklyRoster()
    {
        _weeklyRoster.Clear();

        if (prisonerDatabase == null || prisonerDatabase.prisoners.Count == 0) return;

        var allAnchors = FindObjectsOfType<CellAnchor>();
        var allCellIds = allAnchors.Select(a => a.cellId).ToList();

        foreach (string cellId in allCellIds)
        {
            // 죄수 템플릿(외형/특성)만 고정
            var randomDef = prisonerDatabase.GetRandomDefinition();
            if (randomDef != null)
            {
                _weeklyRoster[cellId] = randomDef.templateId;
            }
        }
        Debug.Log($"[Schedule] Roster Generated for {allCellIds.Count} cells.");
    }

    // 외부에서 죄수 정보(Trait 등) 확인할 때 사용
    public PrisonerDefinition GetAssignedPrisonerDef(string cellId)
    {
        if (_weeklyRoster.TryGetValue(cellId, out string templateId))
        {
            if (prisonerDatabase.TryGet(templateId, out var def))
                return def;
        }
        return null;
    }

    // -----------------------------------------------------------------------
    // 2. Schedule Logic (오늘 상태는 어떤가? - 매일 변동)
    // -----------------------------------------------------------------------
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

                    // [핵심] 여기서 매일매일 성향을 50:50으로 새로 뽑습니다.
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
        Debug.Log($"[Schedule] Generated Daily Schedules (AIType Randomized Daily).");
    }

    public Dictionary<string, DailyCellAssignment> GetScheduleForDay(int dayNumber)
    {
        var data = _weeklySchedule.Find(x => x.dayNumber == dayNumber);
        return data != null ? data.cellAssignments : new Dictionary<string, DailyCellAssignment>();
    }

    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.Phase == GamePhase.Briefing && GameManager.Instance != null)
        {
            var plan = GetScheduleForDay(GameManager.Instance.CurrentDay);
            if (plan.Count > 0)
            {
                // PrisonManager에게 변경된 구조체(DailyCellAssignment)를 전달
                prisonManager.ApplyDailySchedule(plan);
            }
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
}