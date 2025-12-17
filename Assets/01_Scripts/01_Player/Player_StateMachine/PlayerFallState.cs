using UnityEngine;
public sealed class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, true);
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        Vector3 forceMove = Vector3.zero;

        if (P.ForceReceiver != null)
            forceMove = P.ForceReceiver.ConsumeMove(fdt, false);

        // Fall 상태에서도 Move 호출
        P.Controller.Move(forceMove);
    }
    public override void Tick(float dt)
    {
        if (IsGrounded)
        {
            P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, false);
            SM.ChangeState(SM.Locomotion);
        }
    }
}