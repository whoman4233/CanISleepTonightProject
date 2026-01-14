using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(menuName = "Missions/Type: Suppression (Day 1, 3, 4, 7)")]
public class Mission_SuppressionStrategy : DailyMissionStrategy
{
    [Header("Spawn Rules")]
    public int targetSuspiciousCount;
    public int missionCountDown;
    public PrisonerAIType defaultAI = PrisonerAIType.Good;
    public List<PrisonerAIType> specialAIList;
    public List<VisualAnomalyType> specialVisualList;


    public override void SetupDay(AnomalyDistributor ad, PrisonerScheduleManager sm)
    {
        base.SetupDay(ad, sm);

        // 1. 일단 모든 방을 '기본 상태(Good)'로 초기화 (범인 0명)
        // -> 이렇게 해야 우리가 건드리지 않은 나머지 방들이 정상 설정됨
        sm.AssignRolesForNewDay(suspiciousCount: 0, defaultAI: defaultAI);

        // 2. 활성 방 목록을 가져와서 랜덤으로 섞음 (Shuffle)
        var shuffledCells = sm.GetActiveCellIds().OrderBy(x => Random.value).ToList();

        int cellIndex = 0;

        // 3. Special AI 배정 (리스트에 있는 만큼)
        foreach (var aiType in specialAIList)
        {
            if (cellIndex >= shuffledCells.Count) break;

            string cellId = shuffledCells[cellIndex];

            // AI는 특수, 외형은 정상(None), 의심스러움(True)
            sm.SetDailyRole(cellId, aiType, VisualAnomalyType.None, true);

            cellIndex++;
        }

        // 4. Special Visual 배정 (리스트에 있는 만큼) -> ★ 중복 안 됨 (다음 cellIndex 사용)
        foreach (var visualType in specialVisualList)
        {
            if (cellIndex >= shuffledCells.Count) break;

            string cellId = shuffledCells[cellIndex];

            // AI는 비주얼 전용(없으면 Good), 외형은 특수, 의심스러움(True)
            // 비주얼 전용 AI(VisualAnomalyAction)가 없다면 Bad나 Good 중 선택
            sm.SetDailyRole(cellId, PrisonerAIType.Good, visualType, true);

            cellIndex++;
        }

        Debug.Log($"[Mission] 직접 배정 완료: AI {specialAIList.Count}명 / Visual {specialVisualList.Count}명");
    }

    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        // 여기서 currentScore는 "성공적으로 제압한 죄수 수"
        if (currentScore >= targetScore)
        {
            failReason = "";
            return true;
        }
        failReason = $"위험 요소를 모두 제거하지 못했습니다. ({currentScore}/{targetScore})";
        return false;
    }

    public override bool IsValidPrisoner(string cellId)
    {
        // 1. 스케줄 매니저 확인
        if (PrisonerScheduleManager.Instance == null) return false;

        // 2. ★ 핵심: 올려주신 코드에 있는 이 함수를 호출합니다.
        DailyRoleData role = PrisonerScheduleManager.Instance.GetDailyRole(cellId);

        // 3. AI 조건 확인 (구조체 안의 변수 사용)
        if (specialAIList.Contains(role.dailyAIType))
            return true;

        // 4. Visual 조건 확인 (구조체 안의 변수 사용)
        if (specialVisualList.Contains(role.visualType))
            return true;

        return false;
    }
}