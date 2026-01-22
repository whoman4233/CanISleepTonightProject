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
public enum QTEInputState
{
    Pressed,
    Released
}

public struct QTEInputFeedbackEvent
{
    public QTEInputState State;
}