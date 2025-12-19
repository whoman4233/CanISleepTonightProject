using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HiddenItem/KnifeState")]
public class KnifeStateSO : HiddenItemStateSO
{
    public event Action<bool> OnFoundStateChanged;

    public override void OnFound()
    {
        if (isFound)
            return;

        base.OnFound();

        Debug.Log($"[KnifeStateSO] OnFound 호출 | ID={GetInstanceID()}");
        OnFoundStateChanged?.Invoke(isFound);
    }

    public override void ResetState()
    {
        base.ResetState();
        OnFoundStateChanged?.Invoke(isFound);
    }
}

