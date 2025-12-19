using System;
using UnityEngine;

public abstract class HiddenItemStateSO : ScriptableObject
{
    [SerializeField] protected bool isFound;
    public bool IsFound => isFound;

    public event Action<bool> OnFoundStateChanged;

    public virtual void OnFound()
    {
        if (isFound) return;
        isFound = true;
        OnFoundStateChanged?.Invoke(isFound);
    }

    public virtual void ResetState()
    {
        isFound = false;
        OnFoundStateChanged?.Invoke(isFound);
    }
}
