using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DailyMissionManager : MonoBehaviour
{
    public static DailyMissionManager Instance;

    [Header("Mission Settings")]
    [SerializeField] private List<DailyMissionStrategy> missionScenario; // 1~7일차 SO 리스트

    // 현재 진행 중인 미션 (Read Only)
    public DailyMissionStrategy CurrentMission { get; private set; }

    // 오늘 하루 동안 플레이어가 올린 실적
    private int dailyResolvedCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    // 하루 시작 시 호출 (GameManager 등에서 호출)
    public void StartDay(int dayIndex)
    {
        dailyResolvedCount = 0;

        // 인덱스 안전 검사
        if (missionScenario == null || missionScenario.Count < dayIndex)
        {
            Debug.LogError($"[GameFlow] {dayIndex}일차 미션 데이터가 없습니다!");
            return;
        }

        // 1. 오늘의 미션 갈아끼우기
        CurrentMission = missionScenario[dayIndex - 1];
        Debug.Log($"[GameFlow] Day {dayIndex} 미션 시작: {CurrentMission.title}");

        // 2. 전략 실행 (매니저 세팅)
        CurrentMission.SetupDay(AnomalyDistributor.Instance, PrisonerScheduleManager.Instance);

        // 3. (필요하다면) 스포너에게 최종 소환 명령
        // PrisonerSpawnController.Instance.SpawnAll(); 
    }

    // ========================================================================
    // 🔥 [이벤트 훅] 외부에서 호출하는 점수 신고 전화번호
    // ========================================================================

    // 1. 아이템 찾았을 때 (흉기, 금지물품 등)
    public void NotifyItemFound(string itemTag)
    {
        if (CurrentMission != null)
        {
            // 미션에게 "이런 태그 가진 아이템 찾았어" 라고 전달
            CurrentMission.OnEventTriggered(itemTag);
        }
    }

    // 2. 죄수 제압/해결 했을 때 (소음, 폭동 등)
    public void NotifyPrisonerResolved(string cellId)
    {
        dailyResolvedCount++;
        Debug.Log($"[GameFlow] 죄수 해결 확인! (금일 누적: {dailyResolvedCount})");

        // 혹시 미션 쪽에서 실시간 체크가 필요할 수도 있으니 알림
        if (CurrentMission != null)
            CurrentMission.OnEventTriggered("PrisonerResolved");
    }

    // 3. 하루 결산 (SettlementTrigger에서 호출)
    public bool EvaluateDayResult(out string failReason)
    {
        if (CurrentMission == null)
        {
            failReason = "미션 정보 없음";
            return true;
        }

        // 미션 SO에게 "나 오늘 이만큼(dailyResolvedCount) 했어, 합격이야?" 물어봄
        return CurrentMission.CheckWinCondition(dailyResolvedCount, out failReason);
    }

    // [추가] 외부(테스트 콘솔 등)에서 특정 날짜의 미션 데이터를 요청할 때 사용
    public DailyMissionStrategy GetMissionStrategy(int dayIndex)
    {
        // 1일차 = index 0 이므로 -1 처리
        int listIndex = dayIndex - 1;

        if (missionScenario != null && listIndex >= 0 && listIndex < missionScenario.Count)
        {
            return missionScenario[listIndex];
        }

        Debug.LogWarning($"[DailyMissionManager] {dayIndex}일차 미션 데이터가 없습니다.");
        return null;
    }
}
