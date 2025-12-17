public sealed class PlayerFallState : PlayerState
{
    public PlayerFallState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, true);
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