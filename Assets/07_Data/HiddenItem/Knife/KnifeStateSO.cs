using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HiddenItem/KnifeState")]
public class KnifeStateSO : HiddenItemStateSO
{
    [SerializeField] private bool isFound;
    public bool IsFound => isFound;

    public event Action<bool> OnFoundStateChanged;

    public void OnFound()
    {
        if (isFound)
            return;

        isFound = true;
        Debug.Log($"[KnifeStateSO] OnFound 호출 | ID={GetInstanceID()}");

        OnFoundStateChanged?.Invoke(isFound);
    }

    public void ResetState()
    {
        isFound = false;
    }
}


