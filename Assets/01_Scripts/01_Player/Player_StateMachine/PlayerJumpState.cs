using UnityEngine;

public sealed class PlayerJumpState : PlayerState
{
    private const float MinAirTime = 0.05f;
    private float _timer;

    public PlayerJumpState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        _timer = 0f;

        P.Animator.SetTrigger(P.AnimationData.JumpParameterHash);

        if (P.ForceReceiver != null)
            P.ForceReceiver.SetJumpVelocity(P.Data.AirData.JumpForce);
    }

    public override void Tick(float dt)
    {
        _timer += dt;

        if (_timer >= MinAirTime && !IsGrounded)
        {
            P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, true);
            SM.ChangeState(SM.Fall);
            return;
        }

        if (IsGrounded && _timer >= MinAirTime)
        {
            SM.ChangeState(SM.Locomotion);
        }
    }
}