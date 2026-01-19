using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    public static AnomalyDistributor Instance;

    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private PrisonManager prisonManager;
    [SerializeField] private PrisonerScheduleManager scheduleManager;

    // ★ 날짜 리스트 삭제됨 (Theme 검사로 대체)

    // 오늘 등장 가능한 이상현상 후보군
    private List<AnomalyDefinitionSO> currentDayPool = new List<AnomalyDefinitionSO>();

    private void Awake()
    {
        Instance = this;
        if (scheduleManager == null) scheduleManager = PrisonerScheduleManager.Instance;
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
    }

    // 미션 매니저에서 하루 시작할 때 이 함수를 호출해서 Theme을 세팅해줘야 함
    public void FilterAnomalies(MissionDayTheme dayTheme)
    {
        currentDayPool.Clear();
        if (masterDatabase == null || masterDatabase.defs == null) return;

        foreach (var anomaly in masterDatabase.defs)
        {
            if (anomaly == null) continue;

            // ★ 핵심: Theme이 맞는 것만 풀에 넣는다.
            // (만약 평화로운 날이라 Theme이 Nothing이면 아무것도 안 들어감 -> 자동 0개)
            if ((anomaly.validThemes & dayTheme) != 0)
            {
                currentDayPool.Add(anomaly);
            }
        }

        Debug.Log($"[AnomalyDistributor] 테마({dayTheme}) 필터링 결과: {currentDayPool.Count}개 후보 등록됨.");
    }

    public void DistributeAnomalies()
    {
        // 1. 죄수 데이터 안전장치
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            scheduleManager.GenerateNewResidents();
        }

        var allCellIds = anchorRegistry.GetAllCellIds();

        // 2. 만약 풀이 텅 비어있다면? -> 테마에 맞는 이상현상이 없다는 뜻 -> 범인 배정 스킵
        if (currentDayPool.Count == 0)
        {
            Debug.Log("⚪ [AnomalyDistributor] 오늘의 테마에 맞는 이상현상 후보가 없습니다. (범인 배정 없음)");
            // 여기서 리턴하지 않고 돌더라도 아래 로직에서 알아서 걸러짐
        }

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            anchor.ClearDailyAnomalies(); // 초기화

            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            var dailyRole = scheduleManager.GetDailyRole(cellId);
            PrisonerType pType = (pData != null && pData.definition != null) ? pData.definition.traitType : PrisonerType.None;

            // =============================================================
            // ★ 범인(Culprit) 배정 로직
            // 조건 1: 죄수가 용의자(Suspicious)여야 함
            // 조건 2: 풀(Pool)에 줄 수 있는 아이템이 있어야 함
            // =============================================================
            if (dailyRole.isSuspicious && currentDayPool.Count > 0)
            {
                // 1. 죄수 타입에 맞는 개별(Individual) 아이템 우선 검색
                var missionCandidates = currentDayPool
                    .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                    .ToList();

                // 2. 없으면 공통(Common) 아이템에서 검색
                if (missionCandidates.Count == 0)
                {
                    missionCandidates = currentDayPool.Where(d => d.category == AnomalyCategory.Common).ToList();
                }

                // 3. 최종 배정
                if (missionCandidates.Count > 0)
                {
                    var culprit = missionCandidates[Random.Range(0, missionCandidates.Count)];

                    // 리스트에 추가 (SpawnController가 이걸 보고 Suspicious로 생성)
                    anchor.currentDailyAnomalies.Add(culprit);

                    Debug.Log($"🔴 {cellId} ({pType}) -> 범인 확정: {culprit.name}");
                }
                else
                {
                    // 풀에는 있는데, 이 죄수 타입에 맞는 게 없는 경우
                    Debug.LogWarning($"⚠️ {cellId} ({pType}) -> 용의자지만 타입에 맞는 이상현상이 풀에 없음.");
                }
            }
            else
            {
                // 용의자가 아니거나, 줄 아이템이 없는 날
                // 아무것도 안 함 -> SpawnController가 Normal/Decorative만 깔아줌
            }
        }
    }
}