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
        if (Input.GetKeyDown(KeyCode.Alpha1)) StartTestMission(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) StartTestMission(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) StartTestMission(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) StartTestMission(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) StartTestMission(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) StartTestMission(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) StartTestMission(7);
    }

    public void StartTestMission(int day)
    {
        Debug.Log($"<color=cyan>[TEST] Mission {day} (고정) 테스트 시작</color>");

        if (spawnController == null || scheduleManager == null || missionManager == null)
        {
            Debug.LogError("[Debug] 매니저 연결을 확인하세요!");
            return;
        }

        // 1. 초기화
        spawnController.ClearAllForNewDay();
        scheduleManager.ForceRebuildDatabase();

        // 2. 미션 주입 (원본 리스트의 해당 번호 미션을 강제로 세팅)
        // ★ 수정: StartFixDay는 1-based index를 받으므로 'day'를 그대로 넘깁니다.
        missionManager.StartFixDay(day);

        // 3. 전략 가져오기
        var strategy = missionManager.CurrentMission;

        if (strategy == null)
        {
            Debug.LogError($"[Debug] Mission {day} 데이터를 로드하지 못했습니다.");
            return;
        }

        // 4. 죄수 스폰 및 미션 시작
        // (참고: SetupDay와 DistributeAnomalies는 StartFixDay 안에서 이미 호출됨)
        spawnController.SpawnAllPrisoners();
        strategy.OnMissionStart();

        Debug.Log($"<color=green>[TEST] Mission {day} : [{strategy.title}] 세팅 완료!</color>");
    }
}