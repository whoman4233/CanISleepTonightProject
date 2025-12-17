public sealed class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        P.Animator.SetTrigger(P.AnimationData.DieParameterHash);
    }
}