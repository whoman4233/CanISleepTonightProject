
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
//경고 텍스트 팝업
//==========================================
public struct ShowTimedTextPopupEvent 
{
    public string Message;
    public float Duration;

    public ShowTimedTextPopupEvent(string message, float duration = 1f)
    {
        Message = message;
        Duration = duration;
    }
}

//==========================================
//보고서 팝업
//==========================================
public struct ShowSettlementConfirmPopupEvent // 보고서 제출 확인용 이벤트
{

}

public struct SettlementConfirmAcceptedEvent // 예 -> 보고서 팝업 활성화
{

}

public struct SettlementConfirmCancelledEvent // 아니오 -> 이전 상황으로 돌아가기
{

}

public struct PopupCloseRequestedEvent
{

}

//==========================================
//WhiteBoadrd이벤트 목록
//==========================================
public struct ShowWhiteBoardPopupEvent
{

}

public struct HideWhiteBoardPopupEvent
{

}

//화이트보드 데이터
public struct SettlementUIDataCreatedEvent
{
    public SettlementUIData Data;

    public SettlementUIDataCreatedEvent(SettlementUIData data)
    {
        Data = data;
    }
}


// Popup UI ESC로 닫을 때

//==========================================
//ResultUI 이벤트 목록
//==========================================

public struct ResultUIShowRequestedEvent // 정산 데이터
{
    public SettlementResultUIData Data;

    public ResultUIShowRequestedEvent(SettlementResultUIData data)
    {
        Data = data;
    }
}


//==========================================
//InGameMenu 이벤트 목록
//==========================================

public struct PauseGameRequestedEvent // 게임 일시정지
{

}
public struct PauseMenuToggleRequestedEvent // 게임메뉴 켜졌을 때 게임매니저 참고용 이벤트(Toggle)
{

}

public struct ResumeGameRequestedEvent //게임 재개
{

}

public struct ReturnToTitleRequestedEvent //타이틀로 돌아가기
{

}

// --------------------
// Pause Menu (Request)
// --------------------
public struct PauseMenuOpenRequestedEvent { }   // 입력 의도
public struct PauseMenuCloseRequestedEvent { }  // 입력 의도

// --------------------
// Pause Menu (State)
// --------------------
public struct PauseMenuOpenedEvent { }          // 결과(사실)
public struct PauseMenuClosedEvent { }          // 결과(사실)

//==========================================
// 인풋모드 변환 이벤트
//==========================================
public struct InputModeChangedEvent
{
    public InputMode Mode;

    public InputModeChangedEvent(InputMode mode)
    {
        Mode = mode;
    }
}

public struct InputHardResetEvent
{

}

//==========================================
// UI 종료 이벤트 (메인메뉴(인트로신)으로 돌아갈 때)
//==========================================
public struct UIHardResetEvent
{
}

//==========================================
// 감방 관련 UI 이벤트 (HUD 수신)
//==========================================
public struct CellInspectionInProgressEvent
{
    public string CellId;
}

public struct CellInspectionCompletedEvent
{
    public string CellId;
}

