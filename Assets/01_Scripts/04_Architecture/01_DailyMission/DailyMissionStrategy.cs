using UnityEngine;
using System.Collections.Generic;

public abstract class DailyMissionStrategy : ScriptableObject
{
    [Header("Basic Info")]
    public string title;
    [TextArea] public string description;
    [SerializeField] public string missionId;

    [Header("Day Theme")]
    public MissionDayTheme missionTheme; // 오늘 활성화할 테마 비트 (이상현상 필터링용)

    [Header("Goals")]
    public int targetScore; // 목표 점수 (찾아야 할 개수 등)

    [Header("Time Settings")]
    public float missionTimeLimit = 480f; // 기본 480초, 1일차는 짧게 300초?

    [Header("Goals")]
    public string GoalName;

    [Header("Text")]
    [SerializeField] private int missionTextNo;
    public int MissionTextNo => missionTextNo;
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDailyTimeLimit(missionTimeLimit);
        }
    }

    // 2. 이벤트 발생 시 처리 (외부에서 호출: 예 - "흉기 찾음", "소음 해결")
    public virtual void OnEventTriggered(string eventCode) { }

    // 3. 결산 (성공 여부 판단)
    public virtual bool CheckWinCondition(int currentScore, out string failReason)
    {
        failReason = "";
        return currentScore >= targetScore;
    }

    // 미션 시작 시 호출 (4일차 스폰, 타이머 시작 등)
    public virtual void OnMissionStart() { }

    // 미션 종료 시 호출 (정리용)
    public virtual void OnMissionEnd() // true면 미션 성공 대화 이벤트 발행
    {
        DialogueKeys.DialogueType resultType = CheckWinCondition(DailyMissionManager.Instance.CurrentScore, out _)
                ? DialogueKeys.DialogueType.Complete
                : DialogueKeys.DialogueType.Fail;

        // 미션성공여부에 따른 대화이벤트 발행
        EventBus.Publish(new DialogueStepChangedEvent(resultType));
    }

    public virtual string GetProcessedText(string rawText) // 이름 치환 로직. override하지 않으면 그대로 출력
    {
        return rawText;
    }

    // [추가] 아이템이 목표에 맞는지 검사
    public virtual bool IsValidItem(string itemTag)
    {
        return false; // 기본값은 false (오버라이드 안 하면 점수 안 오름)
    }

    // [추가] 죄수가 목표에 맞는지 검사
    // 죄수의 상태를 확인해야 하므로 PrisonerController 전체를 넘겨받는 게 좋습니다.
    public virtual bool IsValidPrisoner(string cellID)
    {
        return false;
    }
}