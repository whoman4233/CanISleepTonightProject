using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonerScheduleManager : MonoBehaviour
{
    public static PrisonerScheduleManager Instance;

    [Header("References")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry; // 방 목록 파악용

    // ========================================================================
    // [데이터 저장소]
    // 1. 거주자 명부 (Resident Roster): 게임 내내 유지 (체력, 템플릿 등)
    // 2. 오늘의 역할 (Daily Roles): 하루마다 리셋 (범인 여부, AI 타입)
    // ========================================================================

    // 정적 캐시 (씬 이동 시 데이터 유지용)
    private static Dictionary<string, PrisonerData> _cachedResidents;

    // 실제 런타임 사용 변수
    private Dictionary<string, PrisonerData> _residents;
    private Dictionary<string, DailyRoleData> _todayRoles = new Dictionary<string, DailyRoleData>();

    private void Awake()
    {
        Instance = this;

        // 1. 캐시 초기화 확인 (새 게임)
        if (_cachedResidents == null)
        {
            _cachedResidents = new Dictionary<string, PrisonerData>();
            Debug.Log("[Schedule] 새 게임: 거주자 명부 초기화됨");
        }

        // 2. 참조 연결
        _residents = _cachedResidents;

        // 3. 레지스트리 연결 (저장/로드 시스템용)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterScheduleManager(this);
        }
    }

    private void Start()
    {
        // 씬 시작 시, 아직 입주민이 없으면 생성 (1일차 Intro 직후)
        if (_residents.Count == 0)
        {
            GenerateNewResidents();
        }
    }

    // =======================================================================
    // [1] 거주자 관리 (Residents) - 누가 어디 사는가?
    // =======================================================================

    // 게임 시작 시 한 번만 호출됨 (모든 방에 죄수 채워넣기)
    private void GenerateNewResidents()
    {
        _residents.Clear();
        if (prisonerDatabase == null || anchorRegistry == null) return;

        var allAnchors = anchorRegistry.GetAllCellIds();
        foreach (var cellId in allAnchors)
        {
            // DB에서 랜덤 죄수 뽑기
            var def = prisonerDatabase.GetRandomDefinition();
            if (def != null)
            {
                // 기본 데이터 생성 (ID, 체력 등)
                PrisonerData newPrisoner = new PrisonerData(def, PrisonerAIType.Good, cellId);
                _residents[cellId] = newPrisoner;
            }
        }
        Debug.Log($"[Schedule] 신규 입주민 {_residents.Count}명 배치 완료.");
    }

    // 외부에서 특정 방의 죄수 정보를 요청할 때
    public PrisonerData GetPrisonerData(string cellId)
    {
        if (_residents.TryGetValue(cellId, out var data))
        {
            // 데이터 반환 전, 오늘의 역할(AI)을 덮어씌워서 줌
            if (_todayRoles.TryGetValue(cellId, out var role))
            {
                data.RuntimeAIType = role.dailyAIType;
                // data.isSuspicious = role.isSuspicious; // 데이터 클래스에 이 필드가 있다면
            }
            return data;
        }
        return null; // 빈 방
    }

    // 오늘의 역할 정보만 따로 요청할 때
    public DailyRoleData GetDailyRole(string cellId)
    {
        if (_todayRoles.TryGetValue(cellId, out var role)) return role;
        return new DailyRoleData(); // 기본값
    }

    // =======================================================================
    // [2] 일일 역할 배정 (Daily Roles) - 오늘은 누가 무엇을 하는가?
    // =======================================================================

    // 🔥 매일 아침 GameFlowController(Strategy)가 호출해야 함
    public void AssignRolesForNewDay(
    int suspiciousCount,
    PrisonerAIType defaultAI,
    List<PrisonerAIType> specialBehaviors = null,
    List<VisualAnomalyType> specialVisuals = null)
    {
        _todayRoles.Clear();
        var cellIds = _residents.Keys.ToList();

        // 1. 셔플 (랜덤 배정 위함)
        Shuffle(cellIds);

        int assignedSuspicious = 0;

        foreach (var cellId in cellIds)
        {
            DailyRoleData role = new DailyRoleData();

            // A. 범인 배정
            if (assignedSuspicious < suspiciousCount)
            {
                role.isSuspicious = true;
                assignedSuspicious++;
            }
            else
            {
                role.isSuspicious = false;
            }

            // B. AI 행동 패턴 배정
            // 특수 행동 리스트가 있다면 랜덤 부여, 아니면 기본 AI
            if (specialBehaviors != null && specialBehaviors.Count > 0)
            {
                role.dailyAIType = specialBehaviors[UnityEngine.Random.Range(0, specialBehaviors.Count)];
            }
            else
            {
                role.dailyAIType = defaultAI;
            }

            // [추가] 비주얼 배정 로직
            if (specialVisuals != null && specialVisuals.Count > 0)
            {
                // 확률적으로 비주얼 변경 (예: 20% 확률 or 리스트에서 순차 배정)
                // 여기서는 간단하게 "특수 행동을 하는 놈은 비주얼도 바뀐다" 등으로 커스텀 가능
                // 예시: 랜덤 배정
                if (UnityEngine.Random.value < 0.3f)
                    role.visualType = specialVisuals[UnityEngine.Random.Range(0, specialVisuals.Count)];
                else
                    role.visualType = VisualAnomalyType.None;
            }

            _todayRoles[cellId] = role;
        }

        Debug.Log($"[Schedule] 오늘 역할 배정 완료. (범인: {assignedSuspicious}명)");
    }

    // =======================================================================
    // [3] 저장 / 로드 / 초기화 (GameManager 연동)
    // =======================================================================

    public static void ResetStaticData()
    {
        _cachedResidents = null;
    }

    public void ExtractDataForSave(out List<PrisonerSaveData> outRoster, out List<DailyRoleSaveData> outDailyRoles)
    {
        // 1. 명부 저장
        outRoster = new List<PrisonerSaveData>();
        foreach (var kvp in _residents)
        {
            outRoster.Add(new PrisonerSaveData
            {
                cellId = kvp.Key,
                prisonerDefID = kvp.Value.definition.templateId,
                currentHealth = kvp.Value.CurrentHealth,
                isSuppressed = kvp.Value.IsSuppressed
            });
        }

        // 2. 오늘 역할 저장 (중간 저장 시 필요)
        outDailyRoles = new List<DailyRoleSaveData>();
        foreach (var kvp in _todayRoles)
        {
            outDailyRoles.Add(new DailyRoleSaveData
            {
                cellId = kvp.Key,
                roleData = kvp.Value
            });
        }
    }

    public void OverrideScheduleFromSave(List<PrisonerSaveData> rosterData, List<DailyRoleSaveData> dailyData)
    {
        // 1. 명부 복원
        _residents.Clear();
        if (rosterData != null)
        {
            foreach (var pData in rosterData)
            {
                var def = prisonerDatabase.prisoners.Find(p => p.templateId == pData.prisonerDefID);
                if (def != null)
                {
                    PrisonerData newData = new PrisonerData(def, PrisonerAIType.Good, pData.cellId);
                    newData.CurrentHealth = pData.currentHealth;
                    newData.IsSuppressed = pData.isSuppressed;
                    _residents[pData.cellId] = newData;
                }
            }
        }

        // 2. 오늘 역할 복원
        _todayRoles.Clear();
        if (dailyData != null)
        {
            foreach (var dData in dailyData)
            {
                _todayRoles[dData.cellId] = dData.roleData;
            }
        }

        // 캐시 동기화
        _cachedResidents = _residents;
    }

    // =======================================================================
    // Utils
    // =======================================================================
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

// =======================================================================
// [데이터 구조체]
// =======================================================================

[System.Serializable]
public struct DailyRoleData
{
    public bool isSuspicious;
    public PrisonerAIType dailyAIType;
    public VisualAnomalyType visualType; // 🔥 [추가] 오늘 입을 옷/외형
}

[System.Serializable]
public class PrisonerSaveData
{
    public string cellId;
    public string prisonerDefID;
    public float currentHealth;
    public bool isSuppressed;
}

[System.Serializable]
public class DailyRoleSaveData
{
    public string cellId;
    public DailyRoleData roleData;
}