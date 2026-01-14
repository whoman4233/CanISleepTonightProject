using UnityEngine;
using System.Collections.Generic;

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

        // 설정된 AI와 Visual 목록을 스케줄 매니저에게 전달
        sm.AssignRolesForNewDay(
            suspiciousCount: targetSuspiciousCount,
            defaultAI: defaultAI,
            specialBehaviors: specialAIList,
            specialVisuals: specialVisualList
        );
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