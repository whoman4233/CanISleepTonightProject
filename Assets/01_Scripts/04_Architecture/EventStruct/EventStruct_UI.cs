//==========================================
//PopupUI 미션브리핑 이벤트 목록
//==========================================
public struct MissionPopupShowRequestedEvent // 미션팝업용 이벤트
{
    public DailyMissionStrategy mission;

    public MissionPopupShowRequestedEvent(DailyMissionStrategy mission)
    {
        this.mission = mission;
    }
}
public struct MissionRevealedEvent //미션팝업 노출 후 미션 HUD/화이트보드 UI 출력용 이벤트
{
    public DailyMissionStrategy mission;

    public MissionRevealedEvent(DailyMissionStrategy mission)
    {
        this.mission = mission;
    }
}

public struct MissionBriefingConfirmedEvent
{

}

public struct MissionBriefingDialogueEndedEvent
{
    public DailyMissionStrategy mission;
    public MissionBriefingDialogueEndedEvent(DailyMissionStrategy mission) => this.mission = mission;
}

public struct MissionReportDialogueEndedEvent //미션 완료시 NPC 대화 종료 후 결과보고서 뜨는 이벤트
{
    public bool success;
    public string failReason;
    public MissionReportDialogueEndedEvent(bool success, string failReason)
    {
        this.success = success;
        this.failReason = failReason;
    }
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

public struct SettlementReportConfirmedEvent // 결과 대사 진행 트리거 이벤트
{

}

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

//화이트보드 데이터
public struct SettlementUIDataCreatedEvent
{
    public SettlementUIData Data;

    public SettlementUIDataCreatedEvent(SettlementUIData data)
    {
        Data = data;
    }
}

//==========================================
//ResultUI 이벤트 목록
//==========================================

// 결과 보고서 UI 열기 요청
public struct ResultUIShowRequestedEvent
{
    public bool isSuccess;
    public string failReason;

    public ResultUIShowRequestedEvent(bool isSuccess, string failReason)
    {
        this.isSuccess = isSuccess;
        this.failReason = failReason;
    }
}

// 결과 보고서 확인 버튼 클릭
public struct ResultUIConfirmedEvent { }

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

