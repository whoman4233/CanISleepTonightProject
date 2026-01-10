using UnityEngine;

public class PrisonerAnimationEventRelay : MonoBehaviour
{
    // =========================
    // Animation Events
    // =========================

    // 공격 시작 프레임
    public void AE_AttackShakeStart()
    {
        EventBus.Publish(new PrisonerAttackShakeStartEvent());
    }

    // 공격 종료 프레임
    public void AE_AttackShakeEnd()
    {
        EventBus.Publish(new PrisonerAttackShakeEndEvent());
    }
}
