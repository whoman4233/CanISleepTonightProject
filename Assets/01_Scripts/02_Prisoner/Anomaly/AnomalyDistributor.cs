using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private PrisonCellManager cellManager; // 죄수 정보 확인용

    [Header("Distribution Settings")]
    [SerializeField] private int commonCount = 2; // 공통 요소 개수
    [SerializeField] private int individualCount = 1; // 개별/특수 요소 개수

    // 하루 시작 시 호출 (매개변수: 현재 폭동 게이지)
    public void DistributeAnomalies(int currentRiotGauge)
    {
        var allCellIds = anchorRegistry.GetAllCellIds();

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;
            var cellData = cellManager.GetCell(cellId);

            // 1. 리스트 초기화
            anchor.currentDailyAnomalies.Clear();

            // 2. 카테고리별 후보군 필터링
            // [공통 후보]
            var commonCandidates = masterDatabase.defs.Where(d => d.category == AnomalyCategory.Common).ToList();

            // [개별/특수 후보]
            // 죄수 타입에 맞거나(Individual) OR 폭동 게이지 조건을 만족하는(Special) 것들
            PrisonerType pType = (cellData != null) ? cellData.AssignedPrisonerType : PrisonerType.None;

            var specialCandidates = masterDatabase.defs.Where(d =>
                (d.category == AnomalyCategory.Individual && d.targetPrisoner == pType) ||
                (d.category == AnomalyCategory.Special && currentRiotGauge >= d.minRiotGauge)
            ).ToList();

            // 3. 랜덤 픽 (중복 방지 셔플)
            Shuffle(commonCandidates);
            Shuffle(specialCandidates);

            // 4. 최종 리스트 구성 (공통 N개 + 개별 M개)
            anchor.currentDailyAnomalies.AddRange(commonCandidates.Take(commonCount));
            anchor.currentDailyAnomalies.AddRange(specialCandidates.Take(individualCount));

            // 디버그
            // Debug.Log($"[Distributor] Cell {cellId}: Assigned {anchor.currentDailyAnomalies.Count} items.");
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int rnd = Random.Range(i, list.Count);
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}