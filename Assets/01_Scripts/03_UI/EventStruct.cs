
//==========================================
//GamePhase관련 이벤트 목록
//==========================================

public struct GamePhaseChangedEvent //게임 페이즈에 따른 UI 변경
{
    public GamePhase Phase;

    public GamePhaseChangedEvent(GamePhase phase)
    {
        Phase = phase;
    }
}

//==========================================
//게임 신 관련 이벤트 목록
//==========================================

public struct GameTimeUpdateEvent //타이머
{
    public float Seconds;
}

//==========================================
//MainMenuUI 이벤트 목록
//==========================================

public struct ShowMainMenuEvent //메인 메뉴 노출
{

}

public struct HideMainMenuEvent //메인 메뉴 숨기기
{

}



public struct StartNewGameEvent // 새 게임 시작
{

}

public struct LoadGameEvent // 이어하기
{

}

//==========================================
//PopupUI 이벤트 목록
//==========================================

public struct ShowExitConfirmPopupEvent //게임 종료 팝업 노출
{

}
public struct ShowSettingsPopupEvent // 옵션팝업 노출
{

}

public struct HideSettingsPopupEvent //옵션 팝업 숨기기
{

}

//==========================================
//InGameMenu 이벤트 목록
//==========================================

public struct PauseMenuToggleRequestedEvent // 게임메뉴 켜졌을 때 게임매니저 참고용 이벤트(Toggle)
{

}

public struct ResumeGameRequestedEvent //게임 재개
{

}

public struct ReturnToTitleRequestedEvent //타이틀로 돌아가기
{

}