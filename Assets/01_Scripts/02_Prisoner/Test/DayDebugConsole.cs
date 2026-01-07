// Assets/01_Scripts/02_Prisoner/Test/DayDebugConsole.cs

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
        if (Input.GetKeyDown(KeyCode.Alpha4)) StartTestMission(4); // 사수 찾기
        if (Input.GetKeyDown(KeyCode.Alpha5)) StartTestMission(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) StartTestMission(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) StartTestMission(7); // 탈옥 저지
    }

    public void StartTestMission(int day)
    {
        Debug.Log($"<color=cyan>[TEST] {day}일차 미션 시뮬레이션 시작</color>");

        if (spawnController == null || scheduleManager == null || missionManager == null)
        {
            Debug.LogError("[Debug] 매니저 연결을 확인하세요!");
            return;
        }

        // ====================================================
        // STEP 1. 월드 클리어 (기존 죄수/프롭 제거)
        // ====================================================
        spawnController.ClearAllForNewDay();

        // ====================================================
        // STEP 2. 데이터 재구축 (빈 방에 새 죄수 채워넣기)
        // ====================================================
        // ★ 이게 없으면 "No prisoner active" 에러 발생함
        scheduleManager.ForceRebuildDatabase();

        // ====================================================
        // STEP 3. 미션 전략 가져오기
        // ====================================================
        var strategy = missionManager.GetMissionStrategy(day);
        if (strategy == null)
        {
            Debug.LogError($"[Debug] {day}일차 미션 데이터(SO)가 세팅되지 않았습니다.");
            return;
        }

        // ====================================================
        // STEP 4. 역할 배정 (SetupDay)
        // ====================================================
        // 여기서 1일차(소음), 4일차(변장), 7일차(AI) 등 규칙이 적용됨
        strategy.SetupDay(anomalyDistributor, scheduleManager);

        // ====================================================
        // STEP 5. 스폰 실행
        // ====================================================
        // 배정된 역할을 기반으로 프리팹/위치 결정하여 소환
        spawnController.SpawnAllPrisoners();

        // ====================================================
        // STEP 6. 미션 시작 트리거 (타이머 등)
        // ====================================================
        strategy.OnMissionStart(); // (가상 함수 추가했으므로 호출 가능)

        Debug.Log($"<color=green>[TEST] {day}일차 세팅 완료!</color>");
    }
}