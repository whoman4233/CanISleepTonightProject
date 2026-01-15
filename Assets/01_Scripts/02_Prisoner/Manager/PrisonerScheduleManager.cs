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

    public void GenerateNewResidents()
    {
        // 1. 초기화 확인
        if (_residents == null) _residents = new Dictionary<string, PrisonerData>();

        // 2. 필수 참조 확인 (여기서 로그가 안 뜨면 연결 문제)
        if (prisonerDatabase == null)
        {
            Debug.LogError("[Schedule] PrisonerDatabaseSO가 연결되지 않았습니다! (Inspector 확인)");
            return;
        }
        if (anchorRegistry == null)
        {
            Debug.LogError("[Schedule] AnchorRegistry가 연결되지 않았습니다! (Inspector 확인)");
            return;
        }

        // 3. 방 목록 가져오기
        var allAnchors = anchorRegistry.GetAllCellIds();

        // ★ [디버깅] 방 목록 개수 출력 (이게 0이면 AnchorRegistry 문제)
        Debug.Log($"[Schedule] AnchorRegistry에서 가져온 방 개수: {allAnchors.Count}");

        foreach (var cellId in allAnchors)
        {
            var def = prisonerDatabase.GetRandomDefinition();
            if (def != null)
            {
                PrisonerData newPrisoner = new PrisonerData(def, PrisonerAIType.Good, cellId);
                _residents[cellId] = newPrisoner;
            }
            else
            {
                Debug.LogWarning("[Schedule] 죄수 정의(Definition)를 가져오지 못했습니다. DB가 비어있나요?");
            }
        }

        // 4. 최종 결과 출력
        Debug.Log($"[Schedule] 신규 입주민 {_residents.Count}명 데이터 생성 완료.");
        _cachedResidents = _residents; // 캐시 동기화
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
        PrisonerAIType defaultAI, // 이건 이제 '대표값'으로만 쓰고, 내부에서 섞음
        List<PrisonerAIType> specialBehaviors = null,
        List<VisualAnomalyType> specialVisuals = null)
    {
        // ★ [안전장치] 만약 Start()보다 미션 설정이 먼저 실행되어 입주민이 없다면 강제 생성
        if (_residents == null || _residents.Count == 0)
        {
            Debug.LogWarning("[Schedule] 역할 배정 시도 중 거주민 명부가 비어있어 재생성합니다.");
            GenerateNewResidents();
        }

        _todayRoles.Clear();

        // 활성 방 목록 가져오기
        var cellIds = GetActiveCellIds();

        // ---------------------------------------------------------------------
        // 1단계: 모든 방에 '기본(Default)' 역할 먼저 배정 (빈 방 방지)
        // ---------------------------------------------------------------------
        foreach (var cellId in cellIds)
        {
            DailyRoleData defaultRole = new DailyRoleData();
            defaultRole.isSuspicious = false;
            defaultRole.visualType = VisualAnomalyType.None;

            // "DefaultAI가 Good이면, Good/Bad를 50:50으로 섞어라" 규칙 적용
            if (defaultAI == PrisonerAIType.Good)
            {
                defaultRole.dailyAIType = (UnityEngine.Random.value > 0.5f) ? PrisonerAIType.Good : PrisonerAIType.Bad;
            }
            else
            {
                defaultRole.dailyAIType = defaultAI; // Good이 아니면 입력받은 값으로 통일
            }

            _todayRoles[cellId] = defaultRole;
        }

        // ---------------------------------------------------------------------
        // 2단계: 목표 개수만큼 '용의자(Suspicious)' 선정 및 덮어쓰기
        // ---------------------------------------------------------------------
        int assignedCount = 0;
        if (suspiciousCount > 0)
        {
            Shuffle(cellIds); // 방 섞기

            for (int i = 0; i < cellIds.Count; i++)
            {
                if (assignedCount >= suspiciousCount) break;

                string targetId = cellIds[i];

                // 기존 값 가져와서 수정 (struct이므로 값 복사됨)
                var role = _todayRoles[targetId];

                role.isSuspicious = true;

                // 2-1. 특수 행동 배정
                if (specialBehaviors != null && specialBehaviors.Count > 0)
                {
                    role.dailyAIType = specialBehaviors[UnityEngine.Random.Range(0, specialBehaviors.Count)];
                }
                else
                {
                    // 특별히 지정된 게 없으면 Bad로 설정 (기본적으로 반항적)
                    role.dailyAIType = PrisonerAIType.Bad;
                }

                // 2-2. 특수 외형 배정 (리스트 순서대로 하나씩)
                if (specialVisuals != null && specialVisuals.Count > 0)
                {
                    // 인덱스 안전 처리 (용의자가 외형 리스트보다 많을 경우 순환)
                    int visualIndex = assignedCount % specialVisuals.Count;
                    role.visualType = specialVisuals[visualIndex];
                }

                _todayRoles[targetId] = role; // 덮어쓰기
                assignedCount++;
            }
        }

        Debug.Log($"[Schedule] 역할 배정 완료. (총 {cellIds.Count}명 중 용의자 {assignedCount}명, 나머지는 기본값)");
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

    // ★ [신규 추가] 테스트용: 명부와 역할을 싹 초기화하고 새로 생성
    public void ForceRebuildDatabase()
    {
        // 1. 기존 데이터 클리어
        _residents.Clear();
        _todayRoles.Clear();

        // 2. 캐시도 클리어 (확실하게)
        ResetStaticData();
        _cachedResidents = _residents;

        // 3. 신규 입주민 생성 (이게 안 되면 스폰할 때 에러 남)
        GenerateNewResidents();

        Debug.Log("[Schedule] 관리자 권한으로 거주자 DB 강제 재구축 완료.");
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

    // ★ [추가] 외부(미션 전략)에서 특정 방의 역할을 직접 지정할 때 사용
    public void SetDailyRole(string cellId, PrisonerAIType aiType, VisualAnomalyType visualType, bool isSuspicious)
    {
        // 딕셔너리가 없으면 생성 (안전장치)
        if (!_todayRoles.ContainsKey(cellId))
        {
            _todayRoles[cellId] = new DailyRoleData();
        }

        DailyRoleData role = _todayRoles[cellId];
        role.isSuspicious = isSuspicious;
        role.dailyAIType = aiType;
        role.visualType = visualType;

        _todayRoles[cellId] = role;
    }

    // ★ [추가] 현재 입주민이 있는 방 목록 반환 (미션에서 섞어 쓸 때 필요)
    public List<string> GetActiveCellIds()
    {
        return _residents.Keys.ToList();
    }

    /// <summary>
    /// 죄수 고유 ID(prisonerId)로 해당 죄수가 살고 있는 방 번호(CellID)를 찾습니다.
    /// </summary>
    public string GetCellIdByPrisonerId(string prisonerId)
    {
        // _residents 딕셔너리를 순회하며 ID가 일치하는 방을 찾습니다.
        foreach (var kvp in _residents)
        {
            if (kvp.Value.ID == prisonerId)
            {
                return kvp.Key; // CellID 반환 (예: "C_1F_01")
            }
        }
        return null; // 못 찾음
    }
    public void ForceTransformPrisoner(string cellId, string targetTemplateId) // 미션6 갱단원만 소환하기 전용 매서드
    {
        if (_residents.ContainsKey(cellId))
        {
            // DB에서 해당 ID("PSN_Gang_01")를 가진 정의를 찾아
            var newDef = prisonerDatabase.prisoners.Find(p => p.templateId == targetTemplateId);

            if (newDef != null)
            {
                // 기존 데이터를 새 정의(갱단원)로 교체함.
                _residents[cellId] = new PrisonerData(newDef, PrisonerAIType.Bad, cellId);
            }
        }
    }
}

// =======================================================================
// [데이터 구조체]
// =======================================================================

[System.Serializable]
public struct DailyRoleData
{
    public bool isSuspicious;            // 범인 여부
    public PrisonerAIType dailyAIType;  // 행동 패턴
    public VisualAnomalyType visualType; // 외형 (비키니, 염소, 임포스터 등)

    //[추가] 생성자: 값을 받아서 초기화
    public DailyRoleData(bool suspicious, PrisonerAIType aiType, VisualAnomalyType visual)
    {
        this.isSuspicious = suspicious;
        this.dailyAIType = aiType;
        this.visualType = visual;
    }
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