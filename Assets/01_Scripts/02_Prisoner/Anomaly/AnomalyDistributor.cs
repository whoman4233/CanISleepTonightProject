using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    // cellManager는 이제 죄수 타입 조회용으로는 안 쓰이지만, 
    // 나중에 폭동 게이지 등 다른 정보 조회용으로 필요할 수 있으니 유지
    [SerializeField] private PrisonManager cellManager;

    [Header("Distribution Settings")]
    [SerializeField] private int commonCount = 2; // 공통 요소 개수
    [SerializeField] private int individualCount = 1; // 개별/특수 요소 개수

    [SerializeField] private PrisonerScheduleManager scheduleManager; // [핵심] 죄수 정보 조회용

    // 하루 시작 시 호출 (매개변수: 현재 폭동 게이지)
    public void DistributeAnomaliesForDay(int currentDay)
    {
        // 폭동 게이지는 GameManager 등에서 가져온다고 가정하거나, 인자로 받으세요.
        // 일단 임시로 0으로 둡니다. 필요시 인자 추가: DistributeAnomalies(int riotGauge)
        int currentRiotGauge = 0;

        var allCellIds = anchorRegistry.GetAllCellIds();

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            // 1. 리스트 초기화
            anchor.ClearDailyAnomalies();

            // 2. 죄수 타입 확인 (ScheduleManager에게 물어봄)
            PrisonerType pType = PrisonerType.None;

            var assignedDef = scheduleManager.GetAssignedPrisonerDef(cellId);
            if (assignedDef != null)
            {
                // [수정] 에러 해결 부분
                // assignedDef.type (Good/Bad)가 아니라, 새로 추가한 특성(traitType)을 가져옵니다.
                pType = assignedDef.traitType;
            }

            // 3. [복구됨] 데이터베이스에서 후보군 필터링 (여기가 없어서 에러 났었음)

            // A. 공통 후보군 (Category == Common)
            var commonCandidates = masterDatabase.defs
                .Where(d => d.category == AnomalyCategory.Common)
                .ToList();

            // B. 특수/개별 후보군
            // (Category == Individual AND 죄수타입 일치) OR (Category == Special AND 폭동게이지 조건 충족)
            var specialCandidates = masterDatabase.defs
                .Where(d =>
                    (d.category == AnomalyCategory.Individual && d.targetPrisoner == pType) ||
                    (d.category == AnomalyCategory.Special && currentRiotGauge >= d.minRiotGauge)
                ).ToList();

            // 4. 랜덤 픽 (중복 방지 셔플)
            Shuffle(commonCandidates);
            Shuffle(specialCandidates);

            // 5. 최종 리스트 구성 (공통 N개 + 개별 M개)
            anchor.currentDailyAnomalies.AddRange(commonCandidates.Take(commonCount));
            anchor.currentDailyAnomalies.AddRange(specialCandidates.Take(individualCount));

            // 디버그
            // Debug.Log($"[Distributor] Cell {cellId} (Type:{pType}): Assigned {anchor.currentDailyAnomalies.Count} items.");
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