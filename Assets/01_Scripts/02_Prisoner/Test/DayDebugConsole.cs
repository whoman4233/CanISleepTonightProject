using UnityEngine;

public class DayDebugConsole : MonoBehaviour
{
    [Header("Managers")]
    public DailyMissionManager missionManager;
    public PrisonerScheduleManager scheduleManager;
    public PrisonerSpawnController spawnController;
    public AnomalyDistributor anomalyDistributor;

    private void Update()
    {
        // 키보드 숫자 1~7 입력 시 테스트 시작
        if (Input.GetKeyDown(KeyCode.Alpha1)) StartTestMission(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) StartTestMission(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) StartTestMission(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) StartTestMission(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) StartTestMission(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) StartTestMission(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) StartTestMission(7);
        // 8번부터는 랜덤 테스트 등 필요에 따라 설정
    }

    public void StartTestMission(int day)
    {
        Debug.Log($"<color=cyan>[TEST] {day}일차 미션 시뮬레이션 시작</color>");

        if (spawnController == null || scheduleManager == null || missionManager == null)
        {
            Debug.LogError("[Debug] 매니저 연결을 확인하세요!");
            return;
        }

        // 1. 초기화
        spawnController.ClearAllForNewDay();
        scheduleManager.ForceRebuildDatabase();

        // ====================================================
        // ★ [핵심 수정] 리스트 인덱스(0-based)에 맞춰 -1 보정
        // ====================================================
        int missionIndex = Mathf.Max(0, day - 1);

        // 2. 미션 주입 (테스트 모드이므로 무조건 고정 미션)
        // (StartFixDay가 내부적으로 missions[index]를 쓴다면 missionIndex를 넘겨야 함)
        missionManager.StartFixDay(missionIndex);

        // 3. 전략 가져오기 (마찬가지로 인덱스 사용)
        var strategy = missionManager.GetMissionStrategy(missionIndex);
        if (strategy == null)
        {
            Debug.LogError($"[Debug] {day}일차(Index: {missionIndex}) 미션 데이터가 없습니다.");
            return;
        }

        // 4. 역할 배정 및 스폰
        strategy.SetupDay(anomalyDistributor, scheduleManager);
        spawnController.SpawnAllPrisoners();
        strategy.OnMissionStart();

        Debug.Log($"<color=green>[TEST] {day}일차(Index {missionIndex}) 세팅 완료!</color>");
    }
}