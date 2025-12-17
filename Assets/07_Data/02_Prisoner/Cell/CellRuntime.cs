using System;

[Serializable]
public class CellRuntime
{
    public string CellId;
    public int Floor;
    public int Number;

    // 오늘 활성 정보
    public bool IsActiveToday;
    public bool IsNoisy;
    public bool IsSuspicious;

    // 점검 흐름
    public bool IsInspectingNow;

    // Suppress 흐름
    public bool IsSuppressing;
    public bool SuppressSuccess;
    public bool NonSuppressChosen;

    // ✅ 오늘 결과
    public bool WasResolvedToday;
    public bool DidSuppress;

    // ✅ 오늘 잠금
    public bool IsLockedForDay;

    public CellState State;

    public void ResetForNewDay()
    {
        IsActiveToday = false;
        IsNoisy = false;
        IsSuspicious = false;
        IsInspectingNow = false;

        IsSuppressing = false;
        SuppressSuccess = false;
        NonSuppressChosen = false;

        WasResolvedToday = false;
        DidSuppress = false;
        IsLockedForDay = false;

        State = CellState.Inactive;
    }
}
