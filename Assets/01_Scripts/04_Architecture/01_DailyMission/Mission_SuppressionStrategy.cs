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

    // 매개변수로 PrisonerController를 받지만, 내부에서는 CellId를 통해 매니저를 조회합니다.
    public override bool IsValidPrisoner(PrisonerController prisoner)
    {
        // 1. 죄수의 방 ID(CellId) 가져오기
        // (PrisonerController에 AssignedCell이 있고, 그 안에 CellId가 있다고 가정)
        if (prisoner == null || prisoner.AssignedCell == null) return false;

        string cellId = prisoner.AssignedCell.cellId;

        // 2. 스케줄 매니저에게 "오늘 이 방 죄수의 역할 정보" 요청
        if (PrisonerScheduleManager.Instance == null) return false;

        // ★ 여기서 핵심: DailyRoleData 구조체를 가져옵니다.
        DailyRoleData roleData = PrisonerScheduleManager.Instance.GetDailyRole(cellId);

        // 3. AI 타입 조건 확인 (리스트에 포함되어 있는지)
        if (specialAIList.Contains(roleData.dailyAIType))
        {
            return true;
        }

        // 4. Visual 타입 조건 확인 (리스트에 포함되어 있는지)
        // DailyRoleData에 visualType이 있으므로 아주 깔끔하게 비교 가능합니다.
        if (specialVisualList.Contains(roleData.visualType))
        {
            return true;
        }

        return false;
    }
}