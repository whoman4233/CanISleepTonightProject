using UnityEngine;

public class DayDebugConsole : MonoBehaviour
{
    public DailyMissionManager missionManager;
    public PrisonerScheduleManager scheduleManager;
    public PrisonerSpawnController spawnController;
    public AnomalyDistributor anomalyDistributor; // 필요 시 추가

    private void Update()
    {
        // 숫자키 1~7을 누르면 해당 날짜 미션 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1)) StartTestMission(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) StartTestMission(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) StartTestMission(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) StartTestMission(4); // 사수 찾기
        if (Input.GetKeyDown(KeyCode.Alpha7)) StartTestMission(7); // 탈옥 저지
    }

    public void StartTestMission(int day)
    {
        Debug.Log($"[Debug] {day}일차 미션 강제 시작 테스트...");

        // 1. 초기화
        spawnController.ClearAllForNewDay();
        PrisonerScheduleManager.ResetStaticData();

        // ★ [추가] 텅 빈 감방에 죄수 다시 채워넣기!
        scheduleManager.GenerateNewResidents();

        // 2. 전략 가져오기
        var strategy = missionManager.GetMissionStrategy(day);
        if (strategy != null)
        {
            // 3. 미션 세팅 (AI 역할 분배 등)
            strategy.SetupDay(anomalyDistributor, scheduleManager);

            // 4. 스폰 실행
            // 4일차는 중앙 스폰, 7일차는 땅굴/기습 등 역할에 맞춰 스폰됨
            spawnController.SpawnAllPrisoners(); // SpawnForCell 루프 도는 함수

            // 5. 미션 시작 트리거
            strategy.OnMissionStart();
        }
        else
        {
            Debug.LogError($"{day}일차 미션 데이터가 없습니다!");
        }
    }
}