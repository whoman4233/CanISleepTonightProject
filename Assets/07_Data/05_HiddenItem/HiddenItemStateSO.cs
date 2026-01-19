using UnityEngine;
using System;

public abstract class HiddenItemStateSO : ScriptableObject
{
    [SerializeField] private bool isFound;
    public bool IsFound => isFound;

    public event Action<bool> OnFoundStateChanged;

    public virtual void OnFound()
    {
        if (isFound) return;
        isFound = true;
        OnFoundStateChanged?.Invoke(isFound);
        EventBus.Publish(new HiddenItemFoundEvent(this as HiddenItemDefinitionSO));
    }

    public virtual void ResetState()
    {
        isFound = false;
        OnFoundStateChanged?.Invoke(isFound);
    }
}

