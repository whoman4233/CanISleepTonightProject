//==========================================
//PopupUI 미션브리핑 이벤트 목록
//==========================================
using UnityEngine;

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
//==========================================
// UI 로딩신에서 숨기기/노출 이벤트
//==========================================

public struct LoadingOverlayShownEvent { }
public struct LoadingOverlayHiddenEvent { }

//==========================================
//미션 브리핑 이벤트
//==========================================

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
//Dialogue UI 이벤트 목록
//==========================================

public struct DialogueStartedEvent
{

}
public struct DialogueEndedEvent
{

}
//==========================================
// 커서 관련 이벤트
//==========================================
public struct CursorOverrideRequestedEvent
{
    public bool HideCursor;
    public CursorLockMode LockMode;
}

public struct CursorOverrideReleasedEvent
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
// 경고 텍스트 팝업
//==========================================
public struct ShowTimedTextPopupEvent
{
    public string MessageId;   //  UITextTableSO의 ID
    public float Duration;
    public bool PlayBeep;

    // 기본: 안내용 (사운드 없음)
    public ShowTimedTextPopupEvent(string messageId, float duration = 1f)
    {
        MessageId = messageId;
        Duration = duration;
        PlayBeep = false;
    }

    // 경고용 명시 생성자
    public ShowTimedTextPopupEvent(
        string messageId,
        float duration,
        bool playBeep
    )
    {
        MessageId = messageId;
        Duration = duration;
        PlayBeep = playBeep;
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
//==========================================
// UI 상태에서 연출용 클릭 이벤트 ex: 결과창 클릭 -> 도장애니메이션 연출
//==========================================
public struct UIProceedRequestedEvent
{
}

//==========================================
//Prompt Text 출력용 이벤트
//==========================================
public struct PromptChangedEvent
{
    public PromptContext context;
    public string promptId; // null or empty → 숨김
}

public class OpenControlGuideEvent // 조작설명 이벤트
{

}
