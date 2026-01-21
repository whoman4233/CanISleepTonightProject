using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PrisonerScheduleManager : MonoBehaviour
{
    public static PrisonerScheduleManager Instance;

    [Header("References")]
    [SerializeField] private PrisonerDatabaseSO prisonerDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;

    private static Dictionary<string, PrisonerData> _cachedResidents;
    private Dictionary<string, PrisonerData> _residents;
    private Dictionary<string, DailyRoleData> _todayRoles = new Dictionary<string, DailyRoleData>();

    private void Awake()
    {
        Instance = this;

        if (_cachedResidents == null)
        {
            _cachedResidents = new Dictionary<string, PrisonerData>();
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
            Debug.LogError("[Schedule] 필수 데이터베이스 또는 레지스트리가 연결되지 않았습니다.");
            return;
        }

        var allAnchors = anchorRegistry.GetAllCellIds();
        Shuffle(allAnchors); // 방 섞기

        Debug.Log($"[Schedule] 방 개수: {allAnchors.Count}, 생성 목표: 4종류(Skinny, Muscular, Gang, Elite) x 3명");

        // --------------------------------------------------------
        // [핵심] 확정 명단(Deck) 만들기
        // --------------------------------------------------------
        List<PrisonerDefinition> spawnDeck = new List<PrisonerDefinition>();

        // ★ 각 타입별 데이터 가져오기 (오타나 데이터 누락 시 에러 로그 발생함)
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Skinny", 3));
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Muscular", 3));
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Gang", 3));
        spawnDeck.AddRange(GetRandomDefinitionsByKeyword("Elite", 3));
        // 주의: 데이터(SO)의 TemplateID에 "Elite"가 포함되어 있어야 함 ("Smart" 등으로 되어있으면 못 찾음)

        // 덱 섞기 (누가 몇 번 방에 갈지 랜덤)
        Shuffle(spawnDeck);

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
            // 2. 방이 남으면 랜덤으로 채움
            else
            {
                def = GetRandomNormalPrisoner();
            }

            if (def != null)
            {
                PrisonerData newPrisoner = new PrisonerData(def, PrisonerAIType.Good, cellId);
                _residents[cellId] = newPrisoner;
            }
        }

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

    // ... (AssignRolesForNewDay 등 기존 로직 유지 - 변경 없음) ...
    public void AssignRolesForNewDay(
        int suspiciousCount,
        PrisonerAIType defaultAI,
        List<PrisonerAIType> specialBehaviors = null,
        List<VisualAnomalyType> specialVisuals = null)
    {
        if (_residents == null || _residents.Count == 0)
        {
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
                defaultRole.dailyAIType = (UnityEngine.Random.value > 0.5f) ? PrisonerAIType.Good : PrisonerAIType.Bad;
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
        Debug.Log($"[Schedule] 역할 배정 완료. (용의자 {assignedCount}명)");
    }

    // ... (저장/로드 관련 기존 코드 유지) ...
    public static void ResetStaticData() { _cachedResidents = null; }

    public void ResetAllSimulationData()
    {
        if (_residents == null) return;
        foreach (var kvp in _residents)
        {
            kvp.Value.CurrentHealth = 100f;
            kvp.Value.IsSuppressed = false;
        }
        _todayRoles.Clear();
        _cachedResidents = _residents;
        Debug.Log("[Schedule] 데이터 리셋 완료 (New Game)");
    }
    // ============================================================
    // "하루 시작" 전용 리셋
    // - 새 게임 리셋과 구분
    // - HP 같은 영구 데이터는 건드리지 않음
    // - 오늘 역할/일일 플래그만 초기화
    // ============================================================
    public void ResetDailyState()
    {
        // 오늘 역할 테이블은 하루 시작 전에 무조건 비워야 함
        _todayRoles.Clear();

        // ★ 팀원 코드에 "일일 제압/잠금" 같은 플래그가 PrisonerData에 있다면
        // 여기서 같이 초기화해야 함.
        // (아래는 예시이므로, 실제 PrisonerData 필드명에 맞게 최소로 적용)
        /*
        foreach (var kvp in _residents)
        {
            kvp.Value.IsDailyLocked = false;
            kvp.Value.WasResolvedToday = false;
        }
        */

        if (_cachedResidents != null)
            _cachedResidents = _residents;

        Debug.Log("[Schedule] ResetDailyState 완료 (TodayRoles cleared)");
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
    // ★ [수정됨] 특정 키워드(Skinny 등)를 가진 죄수를 count만큼 뽑아오는 함수
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
            !p.templateId.Contains("Suspect")
        ).ToList();

        // ★ [핵심 수정] 데이터가 없으면 빨간색 에러를 띄워서 알려줌 (이게 문제 해결의 열쇠)
        if (candidates.Count == 0)
        {
            Debug.LogError($"[Schedule] 데이터 누락: '{keyword}' 키워드를 가진 죄수 데이터(SO)가 0개입니다! PrisonerDatabaseSO를 확인하세요.");
            return result;
        }

        // 2. 랜덤하게 count만큼 뽑기
        for (int i = 0; i < count; i++)
        {
            var randomPick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            result.Add(randomPick);
        }

        return result;
    }

    private PrisonerDefinition GetRandomNormalPrisoner()
    {
        for (int i = 0; i < 10; i++)
        {
            var def = prisonerDatabase.GetRandomDefinition();
            if (def == null) continue;

            string id = def.templateId;
            if (id.Contains("Bikini") || id.Contains("Goat") || id.Contains("Frank") ||
                id.Contains("Suspect") || id.Contains("Victor")) continue;

            return def;
        }
        return prisonerDatabase.GetRandomDefinition();
    }
}

// ... (DailyRoleData 등 구조체는 기존과 동일하게 유지) ...
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