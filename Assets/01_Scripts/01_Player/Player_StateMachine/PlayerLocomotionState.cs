using UnityEngine;

public sealed class PlayerLocomotionState : PlayerState
{
    private const float SpeedDampTime = 0.05f;
    private const float MoveDampTime = 0.05f;
    private const float InputDeadZoneSqr = 0.0001f;

    public PlayerLocomotionState(PlayerStateMachine sm) : base(sm) { }

    public override void Tick(float dt)
    {
        // 앉기 토글 처리 (Ctrl)
        if (P.CrouchToggleRequested)
        {
            P.CrouchToggleRequested = false;

            // 전환중이면 추가 토글 금지
            if (P.IsCrouchTransitioning)
                return;

            // 공중에서는 토글/잠금 금지
            if (!IsGrounded)
                return;

            // 이제부터는 "정상적으로 트리거를 쏠 수 있는 상황"이므로 잠금 시작
            P.BeginCrouchTransitionLock();

            P.IsCrouching = !P.IsCrouching;
            P.Animator.SetBool(P.AnimationData.IsCrouchingParameterHash, P.IsCrouching);

            if (P.IsCrouching)
                P.Animator.SetTrigger(P.AnimationData.CrouchDownParameterHash);
            else
                P.Animator.SetTrigger(P.AnimationData.StandUpParameterHash);
        }

        if (P.Interaction)
        {
            var interactor = P.GetComponent<PlayerInteractor>();
            if (interactor != null)
                interactor.TryInteract();
        }

        if (P.AttackPressedThisFrame)
        {
            SM.ChangeState(SM.Attack);
            return;
        }

        // 앉기/서기 중에는 점프 금지
        if (!P.IsJumpBlockedByCrouch && P.JumpPressedThisFrame && IsGrounded)
        {
            P.JumpLocked = true;
            P.AirFromJump = true;
            P.AirStartY = P.transform.position.y;
            P.AirApexY = P.AirStartY;
            SM.ChangeState(SM.Jump);
            return;
        }

        // 발이 떨어졌는데 점프가 아닌 경우 (계단 끝/낭떠러지)
        if (!IsGrounded)
        {
            P.JumpLocked = true;
            P.AirFromJump = false;
            P.AirStartY = P.transform.position.y;
            P.AirApexY = P.AirStartY;
            SM.ChangeState(SM.Jump);
            return;
        }

        float inputMag = Mathf.Clamp01(P.MoveInput.magnitude);

        // 달리기 입력은 앉아있으면 무시
        bool runAllowed = P.RunHeld && !P.IsCrouchMode;

        P.Sfx?.TickFootstepLoop(dt, P.MoveInput, IsGrounded, runAllowed, P.IsCrouchMode);

        float speedScale = P.IsCrouchMode
            ? P.Data.GroundData.CrouchWalkSpeedModifier
            : (runAllowed ? P.Data.GroundData.RunSpeedModifier : P.Data.GroundData.WalkSpeedModifier);

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
            bool runAllowed = P.RunHeld && !P.IsCrouchMode;

            float modifier = P.IsCrouchMode
                ? P.Data.GroundData.CrouchWalkSpeedModifier
                : (runAllowed ? P.Data.GroundData.RunSpeedModifier : P.Data.GroundData.WalkSpeedModifier);

            float moveSpeed = baseSpeed * modifier;

            horizontalMove = moveDirWorld * moveSpeed * fdt;

            Vector3 moveDirLocal = P.transform.InverseTransformDirection(moveDirWorld);
            moveX = Mathf.Clamp(moveDirLocal.x, -1f, 1f);
            moveY = Mathf.Clamp(moveDirLocal.z, -1f, 1f);
        }

        P.Controller.Move(horizontalMove + verticalMove);

        P.Animator.SetFloat(P.AnimationData.MoveXParameterHash, moveX, MoveDampTime, fdt);
        P.Animator.SetFloat(P.AnimationData.MoveYParameterHash, moveY, MoveDampTime, fdt);

        float targetHeight = P.IsCrouchMode ? P.Data.GroundData.CrouchHeight : P.Data.GroundData.StandingHeight;
        float targetCenterY = P.IsCrouchMode ? P.Data.GroundData.CrouchCenterY : P.Data.GroundData.StandingCenterY;

        float lerpSpeed = P.Data.GroundData.ColliderLerpSpeed;
        float t = 1f - Mathf.Exp(-lerpSpeed * fdt);

        P.Controller.height = Mathf.Lerp(P.Controller.height, targetHeight, t);

        Vector3 c = P.Controller.center;
        c.y = Mathf.Lerp(c.y, targetCenterY, t);
        P.Controller.center = c;
    }
}