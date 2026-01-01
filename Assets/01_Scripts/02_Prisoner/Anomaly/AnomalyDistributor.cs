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
        int currentRiotGauge = prisonManager != null ? GameManager.Instance.CurrentRiotGauge : 0;
        var allCellIds = anchorRegistry.GetAllCellIds();

        // [최적화] 공통/특수 후보군은 밖에서 한 번만 추출
        var globalCommons = masterDatabase.defs.Where(d => d.category == AnomalyCategory.Common).ToList();
        var globalSpecials = masterDatabase.defs.Where(d => d.category == AnomalyCategory.Special && currentRiotGauge >= d.minRiotGauge).ToList();

        foreach (var cellId in allCellIds)
        {
            if (!anchorRegistry.TryGet(cellId, out var anchor)) continue;

            anchor.ClearDailyAnomalies();

            // 이번 감방에서 사용된 타겟(침대, 책상 등)을 추적하는 Set
            HashSet<AnomalyTargetType> usedTargets = new HashSet<AnomalyTargetType>();

            PrisonerDefinition assignedDef = scheduleManager.GetAssignedPrisonerDef(cellId);
            PrisonerType pType = (assignedDef != null) ? assignedDef.traitType : PrisonerType.None;

            // 1. 특수(Special) 먼저 배정 (우선순위 높음)
            // (폭동 포스터가 붙어야 하는데, 일반 포스터 때문에 자리가 없으면 안 되니까)
            var cellSpecials = new List<AnomalyDefinitionSO>(globalSpecials);
            Shuffle(cellSpecials);
            AddUniqueAnomalies(cellSpecials, anchor.currentDailyAnomalies, specialCount, usedTargets);

            // 2. 개별(Individual) 배정
            // (얘는 죄수 특화라 루프 안에서 필터링해야 함)
            var cellIndividual = masterDatabase.defs
                .Where(d => d.category == AnomalyCategory.Individual && d.targetPrisoner == pType)
                .ToList();
            Shuffle(cellIndividual);
            AddUniqueAnomalies(cellIndividual, anchor.currentDailyAnomalies, individualCount, usedTargets);

            // 3. 공통(Common) 배정 (남는 자리에 채워넣기)
            var cellCommons = new List<AnomalyDefinitionSO>(globalCommons);
            Shuffle(cellCommons);
            AddUniqueAnomalies(cellCommons, anchor.currentDailyAnomalies, commonCount, usedTargets);
        }
    }

    // [핵심] 중복 타겟 방지하며 추가하는 함수
    private void AddUniqueAnomalies(List<AnomalyDefinitionSO> source, List<AnomalyDefinitionSO> dest, int maxCount, HashSet<AnomalyTargetType> usedSet)
    {
        int count = 0;
        foreach (var def in source)
        {
            if (count >= maxCount) break;

            // Slot 타입은 여러 개(벽에 포스터 여러 장) 붙을 수 있다고 가정한다면 예외 처리
            // 하지만 침대/변기 같은 'Structure' 교체형은 반드시 하나만 있어야 함
            if (def.targetType != AnomalyTargetType.Slot)
            {
                if (usedSet.Contains(def.targetType)) continue; // 이미 자리 참 -> 패스
                usedSet.Add(def.targetType);
            }

            dest.Add(def);
            count++;
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