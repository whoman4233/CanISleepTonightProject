using UnityEngine;

[CreateAssetMenu(menuName = "HiddenItem/DefinitionSO")]
public class HiddenItemDefinitionSO : HiddenItemStateSO
{
    public override void OnFound()
    {
        if (IsFound) // 부모의 public 프로퍼티 사용
            return;

        base.OnFound();
        Debug.Log($"[KnifeStateSO] OnFound 호출 | ID={GetInstanceID()}");
    }

    public override void ResetState()
    {
        base.ResetState();
    }
}

