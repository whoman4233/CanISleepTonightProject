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

    private List<AnomalyDefinitionSO> currentDayPool = new List<AnomalyDefinitionSO>();

    private void Awake()
    {
        Instance = this;
        if (scheduleManager == null) scheduleManager = PrisonerScheduleManager.Instance;
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
    }

    public void FilterAnomalies(MissionDayTheme dayTheme)
    {
        currentDayPool.Clear();
        if (masterDatabase == null || masterDatabase.defs == null) return;

        foreach (var anomaly in masterDatabase.defs)
        {
            if (anomaly == null) continue;
            if ((anomaly.validThemes & dayTheme) != 0)
            {
                currentDayPool.Add(anomaly);
            }
        }
        Debug.Log($"[AnomalyDistributor] 테마({dayTheme}) 필터링 결과: {currentDayPool.Count}개 후보 등록됨.");
    }

    public void DistributeAnomalies()
    {
        if (scheduleManager.GetActiveCellIds().Count == 0)
        {
            scheduleManager.GenerateNewResidents();
        }

        var allCellIds = anchorRegistry.GetAllCellIds();

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            anchor.ClearDailyAnomalies();

            PrisonerData pData = scheduleManager.GetPrisonerData(cellId);
            var dailyRole = scheduleManager.GetDailyRole(cellId);
            PrisonerType pType = (pData != null && pData.definition != null) ? pData.definition.traitType : PrisonerType.None;

            // =============================================================
            // ★ [수정] 미션 아이템(Common) 누락 방지 로직
            // =============================================================
            if (dailyRole.isSuspicious && currentDayPool.Count > 0)
            {
                // 1. 죄수 전용 아이템
                var individualItems = currentDayPool
                    .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                    .ToList();

                // 2. 공용 아이템 (미션 아이템 포함)
                var commonItems = currentDayPool
                    .Where(d => d.category == AnomalyCategory.Common)
                    .ToList();

                // 3. [핵심] 두 리스트 합치기
                var finalCandidates = new List<AnomalyDefinitionSO>();
                finalCandidates.AddRange(individualItems);
                finalCandidates.AddRange(commonItems);

                if (finalCandidates.Count > 0)
                {
                    var culprit = finalCandidates[Random.Range(0, finalCandidates.Count)];
                    anchor.currentDailyAnomalies.Add(culprit);
                    Debug.Log($"🔴 {cellId} ({pType}) -> 범인 확정: {culprit.name}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ {cellId} ({pType}) -> 용의자지만 배정 가능한 이상현상이 없음.");
                }
            }
        }
    }

    // [추가] 미션 스크립트에서 호출: 특정 아이템 강제 배정
    public void ForceAddAnomaly(string cellId, AnomalyDefinitionSO itemDef)
    {
        if (anchorRegistry.TryGet(cellId, out var anchor))
        {
            anchor.ClearDailyAnomalies(); // 기존 랜덤 배정 삭제 (미션 우선)
            anchor.currentDailyAnomalies.Add(itemDef);
            Debug.Log($"[Mission] {cellId}에 미션 아이템 강제 배정: {itemDef.name}");
        }
    }
}