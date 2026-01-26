using UnityEngine.SceneManagement;

//==========================================
//게임 루프관련 이벤트 목록
//==========================================
public struct GameContextReadyEvent //신 재로딩시 인스턴스가 달라지는걸 확인하는 이벤트
{
    public int CurrentDay;      // 이번 루프/Day의 기준값
    public int MaxDay;
    public GamePhase Phase;

    public GameContextReadyEvent(int currentDay, int maxDay, GamePhase phase)
    {
        CurrentDay = currentDay;
        MaxDay = maxDay;
        Phase = phase;
    }
}
public struct DayChangedEvent // 게임매니저 날짜 변경(HUD Day) 이벤트
{
    public int CurrentDay;

    public DayChangedEvent(int currentDay)
    {
        CurrentDay = currentDay;
    }
}

//==========================================
//게임 신 관련 이벤트 목록
//==========================================

public struct PatrolTimerResetEvent // 타이머 초기화
{
    public float InitialSeconds;

    public PatrolTimerResetEvent(float initialSeconds)
    {
        InitialSeconds = initialSeconds;
    }
}
public struct PatrolTimeoutEvent
{

}

public readonly struct InteractableHoverChangedEvent //크로스 헤어 관련 이벤트(PlayerInteractor Ray 상태 수신)
{
    public readonly bool IsHovering;

    public InteractableHoverChangedEvent(bool isHovering)
    {
        IsHovering = isHovering;
    }
}

//==========================================
// 게임 시작
//==========================================
public struct RequestStartNewGameEvent
{

}

//==========================================
// 이어하기
//==========================================

public struct LoadGameEvent // IntroScene MainMenu : 이어하기
{

}

//==========================================
// 튜토리얼 이후 플레이 씬 진입
//==========================================

public struct IntoPlaySceneEvent
{

}
//근무 실패 후 "새 게임(튜토리얼 스킵)" 재시작 요청 이벤트
public struct RequestRestartFromFailureEvent
{

}

//==========================================
// UI 신 관련 통합관리용 이벤트
//==========================================
public readonly struct SceneChangedEvent
{
    public readonly string SceneName;
    public readonly LoadSceneMode Mode;

    public SceneChangedEvent(string sceneName, LoadSceneMode mode)
    {
        SceneName = sceneName;
        Mode = mode;
    }
}

public struct OutroFinishedEvent //아웃트로 신 종료 (인트로 재진입) 이벤트
{

}