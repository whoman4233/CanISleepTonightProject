using UnityEngine;

[CreateAssetMenu(menuName = "Missions/Type: Interrogation (Day 6)")]
public class Mission_InterrogationStrategy : DailyMissionStrategy
{
    // 범인의 정답 정보 (런타임에 결정됨)
    private string culpritCellId;

    public override void SetupDay(AnomalyDistributor ad, PrisonerScheduleManager sm)
    {
        base.SetupDay(ad, sm); // 테마 필터링 (Interrogation)

        // 1. 범인 1명 지정
        sm.AssignRolesForNewDay(suspiciousCount: 1, defaultAI: PrisonerAIType.Good);

        // 2. ScheduleManager에서 진짜 범인이 누군지 알아내서 저장
        // (ScheduleManager가 Assign 후, 누가 isSuspicious=true인지 찾아오는 기능 필요)
        // 여기선 임시로 "누군가 범인이다"라고 가정하고, 
        // 게임 내 상호작용(대화) 시 이 Strategy의 IsCulprit() 함수를 호출해 확인
    }

    // 대화 시스템에서 호출: "이 죄수가 범인인가요?"
    public bool IsCulprit(string cellId)
    {
        // ScheduleManager에게 물어봄
        var role = PrisonerScheduleManager.Instance.GetDailyRole(cellId);
        return role.isSuspicious;
    }

    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        // 플레이어가 "체포" 버튼을 눌렀을 때, 그 대상이 범인이었는지 확인하는 로직은
        // GameFlowController나 InteractionUI 쪽에서 처리하고
        // 여기서는 최종 결과(성공 횟수 1 이상)만 봅니다.
        if (currentScore >= 1)
        {
            failReason = "";
            return true;
        }
        failReason = "진범을 잡지 못했습니다.";
        return false;
    }
}