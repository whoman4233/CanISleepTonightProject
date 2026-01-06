using UnityEngine;
using System.Collections.Generic;

public abstract class DailyMissionStrategy : ScriptableObject
{
    [Header("Basic Info")]
    public string title;
    [TextArea] public string description;

    [Header("Day Theme")]
    public MissionDayTheme missionTheme; // 오늘 활성화할 테마 비트 (이상현상 필터링용)

    [Header("Goals")]
    public int targetScore; // 목표 점수 (찾아야 할 개수 등)

    public virtual void SetupDay(AnomalyDistributor anomalyDistributor, PrisonerScheduleManager scheduleManager)
    {
        // A. 이상현상 필터링 지시 (Distributor 담당)
        anomalyDistributor.FilterAnomalies(missionTheme);

        // B. 죄수 행동 지시 (ScheduleManager 담당)
        // 기본값: 범인 0명, 모두 평범한 상태(Good/Normal)
        scheduleManager.AssignRolesForNewDay(
            suspiciousCount: 0,
            defaultAI: PrisonerAIType.Good
        );
    }

    // 2. 이벤트 발생 시 처리 (외부에서 호출: 예 - "흉기 찾음", "소음 해결")
    public virtual void OnEventTriggered(string eventCode) { }

    // 3. 결산 (성공 여부 판단)
    public abstract bool CheckWinCondition(int currentScore, out string failReason);
}