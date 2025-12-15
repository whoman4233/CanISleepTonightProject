using System;
using UnityEngine;

[Serializable]
public class CellRuntime
{
    public string CellId;
    public int Floor;   // 1 or 2
    public int Number;  // 1..8

    public bool IsActiveToday;
    public bool IsNoisy;
    public bool IsSuspicious;

    public bool IsInspectingNow;

    // Suppress 흐름 제어
    public bool IsSuppressing;          // 진압 진행 중(퇴장 잠김)
    public bool SuppressSuccess;        // 진압 성공 확정(NotifySuppressSuccess 호출로 true)
    public bool NonSuppressChosen;      // "경고" 버튼을 눌렀는지(선택적으로 로그용)

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

        State = CellState.Inactive;
    }

    public override string ToString()
    {
        return $"{CellId} (F{Floor},#{Number}) Active={IsActiveToday} Noisy={IsNoisy} Susp={IsSuspicious} State={State}";
    }
}
