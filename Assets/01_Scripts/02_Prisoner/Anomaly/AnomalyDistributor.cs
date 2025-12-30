using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AnomalyDistributor : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private AnomalyDatabaseSO masterDatabase;
    [SerializeField] private CellAnchorRegistry anchorRegistry;
    [SerializeField] private PrisonManager prisonManager; // 폭동 게이지 확인용

    [Header("Distribution Settings")]
    [Tooltip("공통 요소(낙서, 파이프 등) 배치 개수")]
    [SerializeField] private int commonCount = 2; 

    [Tooltip("죄수 개인 특성(아령, 책 등) 배치 개수")]
    [SerializeField] private int individualCount = 1;

    [Tooltip("폭동/특수 이벤트(피묻은 벽, 경고문 등) 최대 배치 개수")]
    [SerializeField] private int specialCount = 1; 

    [SerializeField] private PrisonerScheduleManager scheduleManager;

    // 하루 시작 시 호출 (GameManager에서 currentDay와 함께 호출)
    public void DistributeAnomaliesForDay(int currentDay)
    {
        // 1. 현재 폭동 게이지 가져오기
        // (PrisonManager에 CurrentRiotGauge 프로퍼티가 있다고 가정)
        int currentRiotGauge = prisonManager != null ? GameManager.Instance.CurrentRiotGauge : 0;
        
        // 혹은 테스트용 임시 값
        // int currentRiotGauge = 50; 

        var allCellIds = anchorRegistry.GetAllCellIds();

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            // 1. 리스트 초기화
            anchor.ClearDailyAnomalies();

            // 2. 죄수 특성 확인 (ScheduleManager에게 물어봄 - 재사용 데이터)
            PrisonerDefinition assignedDef = scheduleManager.GetAssignedPrisonerDef(cellId);
            PrisonerType pType = (assignedDef != null) ? assignedDef.traitType : PrisonerType.None;

            // -----------------------------------------------------------
            // 3. 후보군 필터링 (로직 분리!)
            // -----------------------------------------------------------

            // A. 공통 (Common) - 누구나 겪음
            var commonCandidates = masterDatabase.defs
                .Where(d => d.category == AnomalyCategory.Common)
                .ToList();

            // B. 개별 (Individual) - 죄수 특성에 맞음 (Ex: Muscular -> 아령)
            var individualCandidates = masterDatabase.defs
                .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                .ToList();

            // C. 특수 (Special) - 폭동 게이지 조건 충족 (Ex: RiotGauge >= 50 -> 폭동 포스터)
            var specialCandidates = masterDatabase.defs
                .Where(d => d.category == AnomalyCategory.Special && currentRiotGauge >= d.minRiotGauge)
                .ToList();

            // -----------------------------------------------------------
            // 4. 랜덤 픽 & 담기
            // -----------------------------------------------------------

            // 셔플 (순서 섞기)
            Shuffle(commonCandidates);
            Shuffle(individualCandidates);
            Shuffle(specialCandidates);

            // A. 공통 요소 N개
            anchor.currentDailyAnomalies.AddRange(commonCandidates.Take(commonCount));

            // B. 개별 요소 M개
            anchor.currentDailyAnomalies.AddRange(individualCandidates.Take(individualCount));

            // C. [추가됨] 특수 요소 K개 (조건이 맞을 때만 추가됨)
            // 이렇게 하면 평소엔 3개(2+1)였다가, 폭동 임박하면 4개(2+1+1)가 됩니다.
            if (specialCandidates.Count > 0)
            {
                anchor.currentDailyAnomalies.AddRange(specialCandidates.Take(specialCount));
            }

            // 디버그
            // Debug.Log($"[Distributor] {cellId}: Common({commonCount}) + Individual({individualCount}) + Special({specialCandidates.Count > 0})");
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