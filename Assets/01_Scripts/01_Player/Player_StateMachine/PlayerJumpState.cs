public class PlayerJumpState : PlayerAirState
{
    public PlayerJumpState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        stateMachine.JumpForce = stateMachine.Player.Data.AirData.JumpForce;
        stateMachine.Player.ForceReceiver.Jump(stateMachine.JumpForce);

        base.Enter();

        StartAnimation(stateMachine.Player.AnimationData.JumpParameterHash);
    }

    public override void Exit()
    {
        base.Exit();
        StopAnimation(stateMachine.Player.AnimationData.JumpParameterHash);
    }
    // Animator Controller 수정하고 돌아오기

    public override void Update()
    {
        base.Update();

        if (stateMachine.Player.Controller.velocity.y <= 0)
        {
            // 추후 Fall 상태로 전환 수정 예정
            stateMachine.ChangeState(stateMachine.IdleState);
            return;
        }
    }
}