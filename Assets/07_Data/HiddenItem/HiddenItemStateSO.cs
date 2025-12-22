using UnityEngine;
using System;

public abstract class HiddenItemStateSO : ScriptableObject
{
    [SerializeField] private bool isFound;
    public bool IsFound => isFound;

    public event Action<bool> OnFoundStateChanged;

    // 자식이 읽을 필요가 있으면 이걸 사용
    protected bool IsFoundInternal => isFound;

    protected void RaiseFoundChanged()
    {
        OnFoundStateChanged?.Invoke(isFound);
    }

    public virtual void OnFound()
    {
        if (isFound)
            return;

        isFound = true;
        RaiseFoundChanged();
    }

    public virtual void ResetState()
    {
        isFound = false;
        RaiseFoundChanged();
    }
}

