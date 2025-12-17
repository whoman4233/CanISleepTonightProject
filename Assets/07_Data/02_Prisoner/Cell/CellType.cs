using System;
using System.Collections.Generic;

public enum ResolveAction
{
    NonSuppress = 0,   // UI상 "경고" = 내부 기록 NonSuppress
    Suppress = 1       // 진압 성공
}

public enum CellState
{
    Inactive = 0,
    ActiveNoisy = 1,
    Inspecting = 2,
    Suppressing = 3,
    LockedForDay = 4
}

[Serializable]
public class ResolvedRecord
{
    public string cellId;
    public bool isSuspicious;
    public bool didSuppress; // true=Suppress 성공, false=NonSuppress(경고)

    public ResolvedRecord(string cellId, bool isSuspicious, bool didSuppress)
    {
        this.cellId = cellId;
        this.isSuspicious = isSuspicious;
        this.didSuppress = didSuppress;
    }
}

[Serializable]
public class UninspectedRecord
{
    public string cellId;
    public bool isSuspicious;

    public UninspectedRecord(string cellId, bool isSuspicious)
    {
        this.cellId = cellId;
        this.isSuspicious = isSuspicious;
    }
}
