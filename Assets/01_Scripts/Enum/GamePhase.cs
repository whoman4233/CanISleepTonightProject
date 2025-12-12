
public enum GamePhase
{
    NotStarted,   // 인트로 화면
    Preparation,  // 준비 페이즈
    Commute,      // 퇴근 페이즈
    Action,       // 액션 페이즈
    Settlement,   // 정산 페이즈
    GoToWork,     // 출근 페이즈
    GameOver      // 게임 오버
}

public enum SleepQuality
{
    None,         // 수면하지 않음
    DeepSleep,    // 숙면 (소음 < 30)
    LightSleep,   // 일반 수면 (30 <= 소음 < 60)
    Impossible    // 수면 불가 (소음 >= 60)
}
