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
}

// ================================
// QTE Lifecycle
// ================================
public struct QTEStartedEvent
{
    public string QTEId;
    public QTEConfig Config;
}

public struct QTEEndedEvent
{
    public string QTEId;
    public QTEResult Result;
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