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

        if (!IsGrounded && P.ForceReceiver != null && P.ForceReceiver.VerticalVelocity < 0f)
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

        // ✅ 수직(중력/groundedGravity/외력)은 입력이 없어도 항상 적용
        Vector3 verticalMove = Vector3.zero;
        if (P.ForceReceiver != null)
            verticalMove = P.ForceReceiver.ConsumeMove(fdt, IsGrounded);

        Vector3 horizontalMove = Vector3.zero;

        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);
        if (input.sqrMagnitude >= 0.0001f)
        {
            Transform cam = Camera.main.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = forward * input.z + right * input.x;

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            P.transform.rotation = Quaternion.Slerp(P.transform.rotation, targetRot, fdt * 10f);

            float baseSpeed = P.Data.GroundData.BaseSpeed;
            float modifier = P.RunHeld
                ? P.Data.GroundData.RunSpeedModifier
                : P.Data.GroundData.WalkSpeedModifier;

            float moveSpeed = baseSpeed * modifier;

            horizontalMove = moveDir * moveSpeed * fdt;
        }

        P.Controller.Move(horizontalMove + verticalMove);
    }
}