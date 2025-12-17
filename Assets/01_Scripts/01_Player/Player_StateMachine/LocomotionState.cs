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

        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);
        if (input.sqrMagnitude < 0.0001f)
            return;

        // 1️⃣ 카메라 기준 방향으로 변환
        Transform cam = Camera.main.transform;

        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDir = forward * input.z + right * input.x;

        // 2️⃣ Player 회전
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        P.transform.rotation = Quaternion.Slerp(
            P.transform.rotation,
            targetRot,
            fdt * 10f // 회전 속도
        );

        // 3️⃣ 이동
        float baseSpeed = P.Data.GroundData.BaseSpeed;
        float modifier = P.RunHeld
            ? P.Data.GroundData.RunSpeedModifier
            : P.Data.GroundData.WalkSpeedModifier;

        float moveSpeed = baseSpeed * modifier;

        P.Controller.Move(moveDir * moveSpeed * fdt);
    }
}