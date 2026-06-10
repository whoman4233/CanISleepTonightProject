using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonerScheduleManager : MonoBehaviour
{
    public static PrisonerScheduleManager Instance;

    [Header("Debug")]
    public bool enableDebugLogs = true; // 로그 온오프용 변수 추가

    [Header("References")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;

    // Good 죄수가 나올 확률 (기본값 0.5 = 50%)
    // Range 속성을 사용하여 유니티 에디터에서 0.0 ~ 1.0 사이의 슬라이더로 조절 가능하게 합니다.
    [Header("AI Ratio Config")]
    [SerializeField, Range(0f, 1f)] private float goodPrisonerRatio = 0.5f;

    private static Dictionary<string, PrisonerData> _cachedResidents;
    private Dictionary<string, PrisonerData> _residents;
    private Dictionary<string, DailyRoleData> _todayRoles = new Dictionary<string, DailyRoleData>();

    private void Awake()
    {
        Instance = this;

        if (_cachedResidents == null)
        {
            _cachedResidents = new Dictionary<string, PrisonerData>();
            if (enableDebugLogs)
                Debug.Log("[Schedule] 새 게임: 거주자 명부 초기화됨");
        }

        _residents = _cachedResidents;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterScheduleManager(this);
        }
    }

    private void Start()
    {
        if (_residents.Count == 0)
        {
            GenerateNewResidents();
        }
    }

    // =======================================================================
    // [1] 거주자 관리 (Residents)
    // =======================================================================

    public void GenerateNewResidents()
    {
        if (_residents == null) _residents = new Dictionary<string, PrisonerData>();

        if (prisonerDatabase == null || anchorRegistry == null)
        {
            if (enableDebugLogs)
                Debug.LogError("[Schedule] 필수 데이터베이스 또는 레지스트리가 연결되지 않았습니다.");
            return;
        }

        var allAnchors = anchorRegistry.GetAllCellIds();
        Shuffle(allAnchors); // 방 섞기

        if (enableDebugLogs)
            Debug.Log($"[Schedule] 방 개수: {allAnchors.Count}, 생성 목표: 4종류(Skinny, Muscular, Gang, Elite) x 3명");

        // --------------------------------------------------------
        // 핵심 확정 명단(Deck) 만들기
        // --------------------------------------------------------
        List<PrisonerDefinition> spawnDeck = new List<PrisonerDefinition>();

        // 각 타입별 데이터 가져오기 (오타나 데이터 누락 시 에러 로그 발생함)
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Skinny", 3));
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Muscular", 3));
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Gang", 3));
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Elite", 3));
        // 주의: 데이터(SO)의 TemplateID에 "Elite"가 포함되어 있어야 함 ("Smart" 등으로 되어있으면 못 찾음)

        // 덱 섞기 (누가 몇 번 방에 갈지 랜덤)
        Shuffle(spawnDeck);

        if (enableDebugLogs)
            Debug.Log($"[Schedule] 생성된 죄수 덱 크기: {spawnDeck.Count}명 (목표: 12명)");

        // 방에 배정
        for (int i = 0; i < allAnchors.Count; i++)
        {
            string cellId = allAnchors[i];
            PrisonerDefinition def = null;

            // 1. 덱에 카드가 남아있다면 덱에서 꺼냄 (균등 배분)
            if (i < spawnDeck.Count)
            {
                def = spawnDeck[i];
            }

            if (def != null)
            {
                PrisonerData newPrisoner = new PrisonerData(def, PrisonerAIType.Good, cellId);
                _residents[cellId] = newPrisoner;
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[Schedule] 신규 입주민 {_residents.Count}명 데이터 생성 완료.");
        _cachedResidents = _residents;
    }

    public PrisonerData GetPrisonerData(string cellId)
    {
        if (_residents.TryGetValue(cellId, out var data))
        {
            if (_todayRoles.TryGetValue(cellId, out var role))
            {
                data.RuntimeAIType = role.dailyAIType;
            }
            return data;
        }
        return null;
    }

    public DailyRoleData GetDailyRole(string cellId)
    {
        if (_todayRoles.TryGetValue(cellId, out var role)) return role;
        return new DailyRoleData();
    }

    public void AssignRolesForNewDay(
        int suspiciousCount,
        PrisonerAIType defaultAI,
        List<PrisonerAIType> specialBehaviors = null,
        List<VisualAnomalyType> specialVisuals = null)
    {
        if (_residents == null || _residents.Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[Schedule] 거주민 명부 비어있음 -> 강제 재생성");
            GenerateNewResidents();
        }

        _todayRoles.Clear();
        var cellIds = GetActiveCellIds();

        // 1. 기본 역할 배정
        foreach (var cellId in cellIds)
        {
            DailyRoleData defaultRole = new DailyRoleData(false, defaultAI, VisualAnomalyType.None);

            if (defaultAI == PrisonerAIType.Good)
            {
                // 0.5 고정값 대신 goodPrisonerRatio 변수를 사용하여 확률 적용
                defaultRole.dailyAIType = (UnityEngine.Random.value <= goodPrisonerRatio) ? PrisonerAIType.Good : PrisonerAIType.Bad;
            }
            _todayRoles[cellId] = defaultRole;
        }

        // 2. 용의자 배정
        int assignedCount = 0;
        if (suspiciousCount > 0)
        {
            Shuffle(cellIds);

            for (int i = 0; i < cellIds.Count; i++)
            {
                if (assignedCount >= suspiciousCount) break;

                string targetId = cellIds[i];
                var role = _todayRoles[targetId];

                role.isSuspicious = true;

                if (specialBehaviors != null && specialBehaviors.Count > 0)
                    role.dailyAIType = specialBehaviors[UnityEngine.Random.Range(0, specialBehaviors.Count)];
                else
                    role.dailyAIType = PrisonerAIType.Bad;

                if (specialVisuals != null && specialVisuals.Count > 0)
                {
                    int visualIndex = assignedCount % specialVisuals.Count;
                    role.visualType = specialVisuals[visualIndex];
                }

                _todayRoles[targetId] = role;
                assignedCount++;
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[Schedule] 역할 배정 완료. (용의자 {assignedCount}명)");
    }

    public static void ResetStaticData() { _cachedResidents = null; }

    public void ResetAllSimulationData()
    {
        // [1] 기존 로직 유지: 각 죄수의 상태값 안전하게 초기화
        // (이 부분은 혹시 모를 참조나 Soft Reset을 위해 남겨두는 것이 좋습니다)
        if (_residents != null)
        {
            foreach (var kvp in _residents)
            {
                kvp.Value.CurrentHealth = 100f;
                kvp.Value.IsSuppressed = false;
                // 필요하다면 일일 플래그도 여기서 확실히 리셋
                kvp.Value.ResetDailyFlags();
            }

            // 인스턴스 데이터 비우기
            // 루프가 끝난 후 리스트를 비워야, 현재 매니저가 들고 있는 낡은 데이터가 사라집니다.
            _residents.Clear();
        }

        _todayRoles.Clear();

        // [핵심 수정] 정적(Static) 캐시 삭제
        // 기존: _cachedResidents = _residents; (이게 문제였습니다. 낡은 데이터를 다시 저장함)
        // 수정: null로 만들어야 다음 게임 시작(Awake) 시 "어? 데이터 없네? 새로 만들자!"가 발동됨.
        _cachedResidents = null;

        if (enableDebugLogs)
            Debug.Log("[Schedule] 데이터 리셋 완료 (New Game - Cache Cleared)");
    }

    // ============================================================
    // "하루 시작" 전용 리셋
    // ============================================================
    public void ResetDailyState()
    {
        // 1. 오늘 역할 테이블 비우기
        _todayRoles.Clear();

        // 2. 모든 죄수의 일일 상태 초기화 호출
        if (_residents != null)
        {
            foreach (var kvp in _residents)
            {
                // 각 죄수 데이터(PrisonerData)에게 "오늘치 상태 리셋해!"라고 명령
                kvp.Value.ResetDailyFlags();
            }
        }

        if (_cachedResidents != null)
            _cachedResidents = _residents;

        if (enableDebugLogs)
            Debug.Log("[Schedule] 모든 죄수의 일일 상태(제압 등)가 초기화되었습니다.");
    }

    public void ExtractDataForSave(out List<PrisonerSaveData> outRoster, out List<DailyRoleSaveData> outDailyRoles)
    {
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

        outDailyRoles = new List<DailyRoleSaveData>();
        foreach (var kvp in _todayRoles)
        {
            outDailyRoles.Add(new DailyRoleSaveData { cellId = kvp.Key, roleData = kvp.Value });
        }
    }

    public void OverrideScheduleFromSave(List<PrisonerSaveData> rosterData, List<DailyRoleSaveData> dailyData)
    {
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

        _todayRoles.Clear();
        if (dailyData != null)
        {
            foreach (var dData in dailyData) _todayRoles[dData.cellId] = dData.roleData;
        }
        _cachedResidents = _residents;
    }

    public void ForceRebuildDatabase()
    {
        _residents.Clear();
        _todayRoles.Clear();
        ResetStaticData();
        _cachedResidents = _residents;
        GenerateNewResidents();

        if (enableDebugLogs)
            Debug.Log("[Schedule] DB 강제 재구축 완료.");
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

    public void SetDailyRole(string cellId, PrisonerAIType aiType, VisualAnomalyType visualType, bool isSuspicious)
    {
        if (!_todayRoles.ContainsKey(cellId)) _todayRoles[cellId] = new DailyRoleData();

        DailyRoleData role = _todayRoles[cellId];
        role.isSuspicious = isSuspicious;
        role.dailyAIType = aiType;
        role.visualType = visualType;
        _todayRoles[cellId] = role;
    }

    public List<string> GetActiveCellIds() { return _residents.Keys.ToList(); }

    public string GetCellIdByPrisonerId(string prisonerId)
    {
        foreach (var kvp in _residents)
        {
            if (kvp.Value.ID == prisonerId) return kvp.Key;
        }
        return null;
    }

    public void ForceTransformPrisoner(string cellId, string targetTemplateId)
    {
        if (_residents.ContainsKey(cellId))
        {
            var newDef = prisonerDatabase.prisoners.Find(p => p.templateId == targetTemplateId);
            if (newDef != null)
            {
                _residents[cellId] = new PrisonerData(newDef, PrisonerAIType.Bad, cellId);
            }
        }
    }

    // =======================================================================
    // 특정 키워드 검색 실패 시 -> 일반 죄수로 대체하여 반환
    // =======================================================================
    private List<PrisonerDefinition> GetRandomDefinitionsByKeyword(string keyword, int count)
    {
        List<PrisonerDefinition> result = new List<PrisonerDefinition>();

        // 1. 해당 키워드를 포함하는 모든 후보군 검색
        var candidates = prisonerDatabase.prisoners.Where(p =>
            p.templateId.Contains(keyword) &&
            !p.templateId.Contains("Frank") &&
            !p.templateId.Contains("Victor") &&
            !p.templateId.Contains("Bikini") &&
            !p.templateId.Contains("Goat") &&
            !p.templateId.Trim().Contains("Suspect")
        ).ToList();

        // 3. 후보가 있다면 정상적으로 랜덤 뽑기
        for (int i = 0; i < count; i++)
        {
            var randomPick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            result.Add(randomPick);
        }

        return result;
    }
}

[System.Serializable]
public struct DailyRoleData
{
    public bool isSuspicious;
    public PrisonerAIType dailyAIType;
    public VisualAnomalyType visualType;

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