// ================================
// QTE Types
// ================================
public enum QTEType
{
    Mash,
    Hold
}

public enum QTEResult
{
    Success,
    Fail,
    Timeout
}

public enum QTEInputState
{
    Pressed,
    Released
}
// ================================
// 죄수 QTE 애니메이션 Enum
// ================================
public enum PrisonerQTEAnimType
{
    Intro,      // QTE 시작 연출 (1회)
    Loop,       // QTE 진행 중 반복
    HitSuccess, // 플레이어 QTE 성공 → 죄수 피격
    AttackFail  // 플레이어 QTE 실패 → 죄수 공격
}
// ================================
// QTE Config
// ================================
public struct QTEConfig
{
    public QTEType Type;

    // 공통
    public float TimeLimit;
    public float RequiredValue;

    // Mash(연타)
    public float PerPressValue;

    // Hold(지속)
    public float HoldPerSecond;

    //입력없을 때 감소량
    public float DecayPerSecond;

    //입력 멈춘 뒤 감소 시작까지 지연
    public float DecayDelay;
}
// ================================
// QTE Lifecycle (SO 기반)
// ================================

public struct QTEStartedEvent
{
    // QTEId + Config 제거
    public QTEActionSO Action;
}

public struct QTEEndedEvent
{
    // QTEId 제거
    public QTEActionSO Action;
    public QTEResult Result;
}
public struct ForceExitInspectionEvent // 상세보기 강제종료
{
}
// ================================
// QTE 결과에 따른 이벤트
// ================================
public struct QTEResultAnimationFinishedEvent // FSM 전환 용 QTE 애니메이션 종료 이벤트
{
    public QTEActionSO Action;
}

public struct PrisonerHitByPlayerEvent //QTE 실패 시 죄수가 플레이어에게 데미지 받음.
{

}

public struct PlayerDamagedEvent // QTE 성공 시 플레이어가 죄수에게 데미지 입힘
{

}

public struct PrisonerHitTimingEvent // 죄수가 플레이어에게 데미지 줌(애니메이션 이벤트용)
{

}

public struct PlayerHitTimingEvent // QTE 실패시 플레이어가 죄수에게 데미지 받음.
{
    public QTEActionSO Action;
}

public struct PlayerAttackTimingEvent // 플레이어의 공격 타이밍 이벤트(애니메이션 이벤트용)
{

}

// ================================
// QTE Progress / Timer
// ================================
public struct QTEProgressChangedEvent
{
    public float Current;
    public float Required;
}

public struct QTETimerChangedEvent
{
    public float Remaining;
    public float Limit;
}
// ================================
// QTE Input Feedback
// ================================
public struct QTEInputFeedbackEvent
{
    public QTEInputState State;
}



