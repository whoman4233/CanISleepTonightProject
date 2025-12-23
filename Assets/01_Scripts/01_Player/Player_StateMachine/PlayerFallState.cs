using UnityEngine;

public sealed class PlayerFallState : PlayerState
{
    private const float AirTurnSpeed = 10f;
    private const float AirControlMultiplier = 0.8f;
    private const float InputDeadZoneSqr = 0.0001f;

    public PlayerFallState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        P.Sfx?.StopFootstepLoopImmediate();
        P.ForceClearCrouchTransitionLock();
        P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, true);
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        // 1) 수직(중력/외력)
        Vector3 verticalMove = Vector3.zero;
        if (P.ForceReceiver != null)
            verticalMove = P.ForceReceiver.ConsumeMove(fdt, false);

        // 2) 수평(공중 조작)
        Vector3 horizontalMove = Vector3.zero;
        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);

        if (input.sqrMagnitude >= InputDeadZoneSqr)
        {
            Transform cam = Camera.main.transform;

            Vector3 forward = cam.forward;
            Vector3 right = cam.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDir = forward * input.z + right * input.x;

            // 공중 회전
            bool rotateOnlyWhenForward = input.z > 0.1f;
            if (rotateOnlyWhenForward && moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                P.transform.rotation = Quaternion.Slerp(P.transform.rotation, targetRot, fdt * AirTurnSpeed);
            }

            // 공중 이동 속도(점프 상태와 동일 컨셉)
            float baseSpeed = P.Data.GroundData.BaseSpeed;
            float modifier = P.RunHeld ? P.Data.GroundData.RunSpeedModifier : P.Data.GroundData.WalkSpeedModifier;
            float moveSpeed = baseSpeed * modifier * AirControlMultiplier;

            horizontalMove = moveDir * moveSpeed * fdt;
        }

        P.Controller.Move(horizontalMove + verticalMove);
    }

    public override void Tick(float dt)
    {
        if (IsGrounded)
        {
            P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, false);
            SM.ChangeState(SM.Land);
        }
    }
}