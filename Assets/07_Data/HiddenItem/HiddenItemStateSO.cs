using UnityEngine;

public abstract class HiddenItemStateSO : ScriptableObject
{
    [SerializeField] private bool isFound;
    public bool IsFound => isFound;

    public virtual void OnFound()
    {
        isFound = true;
    }

    public virtual void ResetState()
    {
        isFound = false;
    }
}
