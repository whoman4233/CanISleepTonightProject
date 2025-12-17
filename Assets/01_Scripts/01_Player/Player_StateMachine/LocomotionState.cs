using UnityEngine;

public sealed class PlayerLocomotionState : PlayerState
{
    private const float SpeedDampTime = 0.05f;

    public PlayerLocomotionState(PlayerStateMachine sm) : base(sm) { }

    public override void Tick(float dt)
    {
        if (P.AttackPressedThisFrame)
        {
            SM.ChangeState(SM.Attack);
            return;
        }

        if (P.JumpPressedThisFrame && IsGrounded)
        {
            SM.ChangeState(SM.Jump);
            return;
        }

        if (!IsGrounded)
        {
            SM.ChangeState(SM.Fall);
            return;
        }

        float speed01 = Mathf.Clamp01(P.MoveInput.magnitude);
        P.Animator.SetFloat(P.AnimationData.SpeedParameterHash, speed01, SpeedDampTime, dt);
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);

        float baseSpeed = P.Data.GroundData.BaseSpeed;
        float modifier = P.RunHeld
            ? P.Data.GroundData.RunSpeedModifier
            : P.Data.GroundData.WalkSpeedModifier;

        float moveSpeed = baseSpeed * modifier;

        Vector3 horizontal = input * moveSpeed;

        Vector3 forceMove = Vector3.zero;
        if (P.ForceReceiver != null)
            forceMove = P.ForceReceiver.ConsumeMove(fdt, IsGrounded);

        P.Controller.Move((horizontal * fdt) + forceMove);
    }
}