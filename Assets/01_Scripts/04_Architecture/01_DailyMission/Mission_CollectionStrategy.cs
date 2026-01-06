using UnityEngine;

[CreateAssetMenu(menuName = "Missions/Type: Collection (Day 2, 5)")]
public class Mission_CollectionStrategy : DailyMissionStrategy
{
    [Header("Collection Settings")]
    public string targetItemTag; // 예: "Weapon", "Contraband"

    public override void SetupDay(AnomalyDistributor ad, PrisonerScheduleManager sm)
    {
        // 1. 이상현상 배포 (이날 테마에 맞는 아이템들이 깔림)
        base.SetupDay(ad, sm);

        // 2. 죄수들은 얌전하게 설정
        sm.AssignRolesForNewDay(suspiciousCount: 0, defaultAI: PrisonerAIType.Good);
    }

    // 아이템을 찾았을 때(클릭했을 때) 호출됨
    public override void OnEventTriggered(string eventCode)
    {
        // 예: eventCode가 "Found_Knife" 라면
        if (eventCode.Contains(targetItemTag))
        {
            // 점수 증가 로직은 GameFlowController 등에서 관리한다고 가정하고
            // 여기선 로그만 찍거나, Strategy 내부에 count 변수를 둬도 됨
            Debug.Log($"[Mission] 목표 아이템 발견! ({eventCode})");
        }
    }

    public override bool CheckWinCondition(int currentScore, out string failReason)
    {
        if (currentScore >= targetScore)
        {
            failReason = "";
            return true;
        }
        failReason = $"목표 물품을 {targetScore}개 찾아야 합니다. (현재: {currentScore})";
        return false;
    }
}