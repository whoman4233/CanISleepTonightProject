using UnityEngine;
using System;
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
public struct RiotGaugeChangedEvent // 폭동게이지 변화 데이터 전달용 이벤트
{
    public int Current;
    public int Max;

    public RiotGaugeChangedEvent(int current, int max)
    {
        Current = current;
        Max = max;
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

public struct RequestPhaseChangeEvent // 페이즈 변경 요청
{
    public GamePhase TargetPhase;
    public RequestPhaseChangeEvent(GamePhase phase) => TargetPhase = phase;
}

public struct EndingConditionMetEvent // 엔딩 조건 달성 알림
{
    public GameEndingType EndingType;
    public EndingConditionMetEvent(GameEndingType type) => EndingType = type;
}
public struct SettlementStartedEvent // 정산페이즈 시작알림
{

}
public struct SettlementCompletedEvent // 정산페이즈 종료알림
{
    public bool IsEnding;
}

public struct SettlementConfirmedEvent // UI에서 사용할 정산페이즈 확인 이벤트
{

}

public struct RequestSceneReloadEvent // 씬 재로딩 요청 이벤트
{

}

public struct EndingUIShowRequestedEvent
{
    public EndingUIData Data;
    public EndingUIShowRequestedEvent(EndingUIData data)
    {
        Data = data;
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

public readonly struct InteractableHoverChangedEvent //크로스 헤어 관련 이벤트(PlayerInteractor Ray 상태 수신)
{
    public readonly bool IsHovering;

    public InteractableHoverChangedEvent(bool isHovering)
    {
        IsHovering = isHovering;
    }
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

// --------------------
// Global Input Lock (Request)
// --------------------
public struct GlobalInputLockRequestedEvent     //  요청
{
    public GlobalInputLockReason Reason;
    public GlobalInputLockRequestedEvent(GlobalInputLockReason reason) => Reason = reason;
}

public struct GlobalInputLockReleasedEvent      // 
{
    public GlobalInputLockReason Reason;
    public GlobalInputLockReleasedEvent(GlobalInputLockReason reason) => Reason = reason;
}

// --------------------
// Player 유무 파악
// --------------------
public struct PlayerPresenceChangedEvent         // 
{
    public bool IsPresent;
    public PlayerPresenceChangedEvent(bool isPresent) => IsPresent = isPresent;
}

//==========================================
//Inspection(상세보기) 이벤트 목록
//==========================================

public struct InspectionRequestedEvent //상세보기 요청
{
    public IInspectable Target;
}

public struct InspectionStartedEvent // 상세보기 시작
{
    public IInspectable Target;
}

public struct InspectionEndedEvent // 상세보기 종료
{

}


public struct InspectionViewReadyEvent // 상세보기 상호작용을 위한 View 이벤트
{

}

public struct InspectionViewRequestedEvent
{

}

public struct InspectionViewReleasedEvent
{

}

//==========================================
// 게임 시작 관련 이벤트
//==========================================
public struct RequestStartNewGameEvent
{

}

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
// 플레이어 HP 변경 이벤트 (HUD 수신)
//==========================================
public struct PlayerHpChangedEvent
{
    public int CurrentHp;

    public PlayerHpChangedEvent(int currentHp)
    {
        CurrentHp = currentHp;
    }
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
