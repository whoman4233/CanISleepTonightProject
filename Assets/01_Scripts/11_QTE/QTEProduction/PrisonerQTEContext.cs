using UnityEngine;

public static class PrisonerQTEContext
{
    public static Transform CurrentAttacker { get; private set; }
    public static PrisonerQTEAnimator CurrentAttackerAnimator { get; private set; }

    public static void SetAttacker(Transform attacker)
    {
        CurrentAttacker = attacker;
        CurrentAttackerAnimator = attacker != null ? attacker.GetComponent<PrisonerQTEAnimator>() : null;
    }

    public static void Clear()
    {
        CurrentAttacker = null;
        CurrentAttackerAnimator = null;
    }
}
