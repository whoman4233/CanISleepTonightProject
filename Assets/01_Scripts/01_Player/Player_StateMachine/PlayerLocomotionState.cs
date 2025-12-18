using UnityEngine;

public sealed class PlayerLocomotionState : PlayerState
{
    private const float SpeedDampTime = 0.05f;
    private const float MoveDampTime = 0.05f;
    private const float InputDeadZoneSqr = 0.0001f;

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

        // "걷는데도 뛰는 애니" 방지용: RunHeld에 따라 스케일을 바꿔서 Animator에 전달
        float inputMag = Mathf.Clamp01(P.MoveInput.magnitude);
        float speedScale = P.RunHeld ? 1f : 0.5f;   // 필요하면 Data로 빼도 됨
        float speedParam = inputMag * speedScale;

        P.Animator.SetFloat(P.AnimationData.SpeedParameterHash, speedParam, SpeedDampTime, dt);
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        Vector3 verticalMove = Vector3.zero;
        if (P.ForceReceiver != null)
            verticalMove = P.ForceReceiver.ConsumeMove(fdt, IsGrounded);

        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);

        Vector3 horizontalMove = Vector3.zero;
        float moveX = 0f;
        float moveY = 0f;

        if (input.sqrMagnitude >= InputDeadZoneSqr)
        {
            Transform cam = Camera.main.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirWorld = (forward * input.z + right * input.x).normalized;

            float baseSpeed = P.Data.GroundData.BaseSpeed;
            float modifier = P.RunHeld ? P.Data.GroundData.RunSpeedModifier : P.Data.GroundData.WalkSpeedModifier;
            float moveSpeed = baseSpeed * modifier;

            horizontalMove = moveDirWorld * moveSpeed * fdt;

            // ✅ 애니메이션용 방향: 플레이어 로컬 기준으로 변환해서 MoveX/MoveY로 보냄
            Vector3 moveDirLocal = P.transform.InverseTransformDirection(moveDirWorld);
            moveX = moveDirLocal.x;
            moveY = moveDirLocal.z;
        }

        // ✅ LocomotionState에서 회전은 하지 않는다 (360도/덜덜의 핵심 제거)
        P.Controller.Move(horizontalMove + verticalMove);

        // 애니메이터 방향 파라미터 업데이트
        P.Animator.SetFloat(P.AnimationData.MoveXParameterHash, moveX, MoveDampTime, fdt);
        P.Animator.SetFloat(P.AnimationData.MoveYParameterHash, moveY, MoveDampTime, fdt);
    }
}