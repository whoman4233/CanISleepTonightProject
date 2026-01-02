using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonerScheduleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;

    [Header("Settings")]
    [SerializeField] private int daysInWeek = 7;
    [SerializeField] private int dailyActiveCount = 6;
    [SerializeField] private int dailySuspiciousCount = 3;

    // ========================================================================
    // [핵심] 씬이 바뀌어도 살아있는 데이터 저장소 (Static Cache)
    // ========================================================================
    private static Dictionary<string, PrisonerData> _cachedWeeklyInstances;
    private static List<DailyScheduleData> _cachedWeeklySchedule;

    // 현재 인스턴스에서 쓰는 참조 변수
    private Dictionary<string, PrisonerData> _weeklyInstances;
    private List<DailyScheduleData> _weeklySchedule;

    private Dictionary<string, DailyCellAssignment> _todayCache = new Dictionary<string, DailyCellAssignment>();
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterScheduleManager(this);
        }
        else
        {
            Debug.LogError("GameManager를 찾을 수 없습니다! IntroScene부터 시작했나요?");
        }

        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
        _onPhaseChanged = HandleGamePhaseChanged;

        // 1. 캐시(Static)가 비어있으면? -> 새 게임 (또는 초기화)
        if (_cachedWeeklyInstances == null || _cachedWeeklySchedule == null)
        {
            _cachedWeeklyInstances = new Dictionary<string, PrisonerData>();
            _cachedWeeklySchedule = new List<DailyScheduleData>();

            // 여기서 바로 생성하지 않고, GameManager가 Load를 시도한 뒤에 
            // 데이터가 없으면 Generate하도록 유도하거나, 
            // 일단 비워두고 Start에서 판단합니다.
        }

        // 2. 현재 인스턴스에 정적 데이터 연결 (참조 공유)
        _weeklyInstances = _cachedWeeklyInstances;
        _weeklySchedule = _cachedWeeklySchedule;
    }

    private void Start()
    {
        // 씬 로드 후, 데이터가 텅 비어있다면 (새 게임 시작 1일차) -> 생성!
        // (만약 로드된 게임이라면 이미 데이터가 채워져 있을 것임)
        if (_weeklyInstances.Count == 0)
        {
            GenerateWeeklyRoster();
            GenerateWeeklySchedule();
            Debug.Log("[Schedule] 새 게임 스케줄 생성 완료 (Static Cache 저장됨)");
        }
        else
        {
            Debug.Log("[Schedule] 기존 스케줄 데이터 유지됨 (Day 2+ or Loaded)");
        }

        // 오늘자 캐시 갱신
        if (GameManager.Instance != null)
        {
            GetAssignmentsForDay(GameManager.Instance.CurrentDay);
        }
    }

    private void OnEnable() => EventBus.Subscribe(_onPhaseChanged);
    private void OnDisable() => EventBus.Unsubscribe(_onPhaseChanged);

    // =======================================================================
    // [외부 호출] GameManager용 저장/로드/초기화 함수
    // =======================================================================

    // 게임 완전히 껐다가 켜거나, 메인 메뉴로 나갈 때 호출 필요 (데이터 초기화)
    public static void ResetStaticData()
    {
        _cachedWeeklyInstances = null;
        _cachedWeeklySchedule = null;
        Debug.Log("[Schedule] 정적 데이터 초기화됨");
    }

    // 파일 로드 시 호출: 저장된 데이터로 덮어쓰기
    public void OverrideScheduleFromSave(List<PrisonerSaveData> rosterData, List<DailyScheduleSaveData> scheduleData)
    {
        if (rosterData == null || rosterData.Count == 0) return;

        _weeklyInstances.Clear();
        _weeklySchedule.Clear();

        // 1. 명부 복원
        foreach (var pData in rosterData)
        {
            // ID로 SO 찾기 (임시로 이름 매칭, ID가 있다면 ID로 하세요)
            var def = prisonerDatabase.prisoners.Find(p => p.templateId == pData.prisonerDefID);
            if (def != null)
            {
                PrisonerData newData = new PrisonerData(def, PrisonerAIType.Good, pData.cellId);
                newData.CurrentHealth = pData.currentHealth;
                newData.IsSuppressed = pData.isSuppressed;
                _weeklyInstances[pData.cellId] = newData;
            }
        }

        // 2. 스케줄 복원
        foreach (var sData in scheduleData)
        {
            DailyScheduleData dayData = new DailyScheduleData { dayNumber = sData.dayNumber };
            foreach (var entry in sData.assignmentList)
            {
                dayData.cellAssignments.Add(entry.cellId, entry.assignment);
            }
            _weeklySchedule.Add(dayData);
        }

        // 3. Static에도 반영 (중요)
        _cachedWeeklyInstances = _weeklyInstances;
        _cachedWeeklySchedule = _weeklySchedule;

        Debug.Log("[Schedule] 세이브 파일 기반으로 스케줄 덮어쓰기 완료");
    }

    // 저장 시 호출: 현재 데이터를 리스트 형태로 반환
    public void ExtractDataForSave(out List<PrisonerSaveData> outRoster, out List<DailyScheduleSaveData> outSchedule)
    {
        outRoster = new List<PrisonerSaveData>();
        outSchedule = new List<DailyScheduleSaveData>();

        foreach (var kvp in _weeklyInstances)
        {
            outRoster.Add(new PrisonerSaveData
            {
                cellId = kvp.Key,
                prisonerDefID = kvp.Value.definition.templateId, // SO 이름 저장
                currentHealth = kvp.Value.CurrentHealth,
                isSuppressed = kvp.Value.IsSuppressed
            });
        }

        foreach (var sch in _weeklySchedule)
        {
            var sSave = new DailyScheduleSaveData { dayNumber = sch.dayNumber };
            foreach (var assign in sch.cellAssignments)
            {
                sSave.assignmentList.Add(new CellAssignmentEntry
                {
                    cellId = assign.Key,
                    assignment = assign.Value
                });
            }
            outSchedule.Add(sSave);
        }
    }

    // =======================================================================
    // [기존 로직 유지]
    // =======================================================================
    public Dictionary<string, DailyCellAssignment> GetAssignmentsForDay(int dayNumber)
    {
        if (dayNumber <= 0) dayNumber = 1;
        var dayData = _weeklySchedule.Find(x => x.dayNumber == dayNumber);

        if (dayData != null)
        {
            _todayCache = dayData.cellAssignments;
            return dayData.cellAssignments;
        }
        return new Dictionary<string, DailyCellAssignment>();
    }

    public PrisonerData GetPrisonerData(string cellId)
    {
        if (!_weeklyInstances.TryGetValue(cellId, out PrisonerData data)) return null;

        if (_todayCache.TryGetValue(cellId, out DailyCellAssignment dailyInfo))
            data.RuntimeAIType = dailyInfo.dailyAIType;
        else
            data.RuntimeAIType = PrisonerAIType.Good;

        return data;
    }

    [ContextMenu("Generate Roster Now")]
    public void GenerateWeeklyRoster()
    {
        _weeklyInstances.Clear();
        if (prisonerDatabase == null) return;
        var allAnchors = FindObjectsOfType<CellAnchor>();
        foreach (var anchor in allAnchors)
        {
            var def = prisonerDatabase.GetRandomDefinition();
            if (def != null) _weeklyInstances[anchor.cellId] = new PrisonerData(def, PrisonerAIType.Good, anchor.cellId);
        }
    }

    public void GenerateWeeklySchedule()
    {
        _weeklySchedule.Clear();
        var ids = _weeklyInstances.Keys.ToList();
        for (int i = 1; i <= daysInWeek; i++)
        {
            DailyScheduleData dayData = new DailyScheduleData { dayNumber = i };
            Shuffle(ids);
            for (int k = 0; k < ids.Count; k++)
            {
                if (k < dailyActiveCount)
                {
                    bool suspicious = k < dailySuspiciousCount;
                    var type = UnityEngine.Random.value > 0.5f ? PrisonerAIType.Good : PrisonerAIType.Bad;
                    dayData.cellAssignments.Add(ids[k], new DailyCellAssignment { isSuspicious = suspicious, dailyAIType = type });
                }
            }
            _weeklySchedule.Add(dayData);
        }
    }

    private void Shuffle<T>(List<T> list) { /* (기존 셔플 코드 유지) */ }
    public PrisonerDefinition GetAssignedPrisonerDef(string cellId)
    {
        if (_weeklyInstances.TryGetValue(cellId, out PrisonerData data)) return data.definition;
        return null;
    }
    private void HandleGamePhaseChanged(GamePhaseChangedEvent evt)
    {
        if (evt.Phase == GamePhase.Briefing && GameManager.Instance != null)
        {
            if (_todayCache.Count == 0) GetAssignmentsForDay(GameManager.Instance.CurrentDay);
        }
    }
}

// =======================================================================
// [저장용 데이터 구조체] (GameManager의 GameSaveData에서 씀)
// =======================================================================

[System.Serializable]
public class PrisonerSaveData
{
    public string cellId;
    public string prisonerDefID;
    public float currentHealth;
    public bool isSuppressed;
}

[System.Serializable]
public class DailyScheduleSaveData
{
    public int dayNumber;
    public List<CellAssignmentEntry> assignmentList = new List<CellAssignmentEntry>();
}

[System.Serializable]
public struct CellAssignmentEntry
{
    public string cellId;
    public DailyCellAssignment assignment;
}

[System.Serializable]
public struct DailyCellAssignment
{
    public bool isSuspicious;       // 오늘 이 죄수가 '범인(이상현상)'인지 여부
    public PrisonerAIType dailyAIType; // 오늘 이 죄수의 기분 (AI 패턴)
}

[System.Serializable]
public class DailyScheduleData
{
    public int dayNumber;
    // 하루치 감방 배정 정보를 담는 딕셔너리
    public Dictionary<string, DailyCellAssignment> cellAssignments = new Dictionary<string, DailyCellAssignment>();
}