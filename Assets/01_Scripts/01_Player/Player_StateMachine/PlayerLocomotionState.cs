using UnityEngine;

public sealed class PlayerLocomotionState : PlayerState
{
    private const float SpeedDampTime = 0.05f;
    private const float MoveDampTime = 0.05f;
    private const float InputDeadZoneSqr = 0.0001f;

    public PlayerLocomotionState(PlayerStateMachine sm) : base(sm) { }

    public override void Tick(float dt)
    {
        // =========================
        // 앉기 토글 처리 (Ctrl)
        // =========================
        if (P.CrouchToggleRequested)
        {
            P.OnCrouchToggleStarted();

            P.CrouchToggleRequested = false;

            // 전환중이면 추가 토글 금지
            if (P.IsCrouchTransitioning)
                return;

            // 공중에서는 토글 금지
            if (!IsGrounded)
                return;

            // 전환 잠금 시작
            P.BeginCrouchTransitionLock();

            // 상태 토글
            P.IsCrouching = !P.IsCrouching;
            P.Animator.SetBool(
                P.AnimationData.IsCrouchingParameterHash,
                P.IsCrouching
            );

            if (P.IsCrouching)
            {
                // 앉기 시작
                P.TargetMoveSpeed =
                    P.Data.GroundData.BaseSpeed *
                    P.Data.GroundData.CrouchWalkSpeedModifier;

                // 여기서는 IsCrouchMode를 아직 true로 두지 않음
            }
            else
            {
                // StandUp 시작 순간에 이미 "앉은 규칙" 해제
                P.ExitCrouchModeEarly();

                bool standRunAllowed = P.RunHeld;
                float standModifier = standRunAllowed
                    ? P.Data.GroundData.RunSpeedModifier
                    : P.Data.GroundData.WalkSpeedModifier;

                P.TargetMoveSpeed =
                    P.Data.GroundData.BaseSpeed * standModifier;
            }

            // 애니메이션 트리거
            if (P.IsCrouching)
                P.Animator.SetTrigger(P.AnimationData.CrouchDownParameterHash);
            else
                P.Animator.SetTrigger(P.AnimationData.StandUpParameterHash);
        }

        // =========================
        // 상호작용
        // =========================
        if (P.Interaction)
        {
            var interactor = P.GetComponent<PlayerInteractor>();
            if (interactor != null)
                interactor.TryInteract();
        }

        // =========================
        // 공격
        // =========================
        if (P.AttackPressedThisFrame)
        {
            SM.ChangeState(SM.Attack);
            return;
        }

        // =========================
        // 점프
        // =========================
        if (!P.IsJumpBlockedByCrouch && P.JumpPressedThisFrame && IsGrounded)
        {
            P.JumpLocked = true;
            P.AirFromJump = true;
            P.AirStartY = P.transform.position.y;
            P.AirApexY = P.AirStartY;
            SM.ChangeState(SM.Jump);
            return;
        }

        // 낙하 감지
        if (!IsGrounded)
        {
            P.JumpLocked = true;
            P.AirFromJump = false;
            P.AirStartY = P.transform.position.y;
            P.AirApexY = P.AirStartY;
            SM.ChangeState(SM.Jump);
            return;
        }

        // =========================
        // Animator Speed 파라미터
        // =========================
        float inputMag = Mathf.Clamp01(P.MoveInput.magnitude);

        bool runAllowed = P.RunHeld && !P.IsCrouchMode;

        P.Sfx?.TickFootstepLoop(
            dt,
            P.MoveInput,
            IsGrounded,
            runAllowed,
            P.IsCrouchMode
        );

        float speedScale = P.IsCrouchMode
            ? P.Data.GroundData.CrouchWalkSpeedModifier
            : (runAllowed
                ? P.Data.GroundData.RunSpeedModifier
                : P.Data.GroundData.WalkSpeedModifier);

        float speedParam = inputMag * speedScale;

        P.Animator.SetFloat(
            P.AnimationData.SpeedParameterHash,
            speedParam,
            SpeedDampTime,
            dt
        );
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        // =========================
        // 수직 이동
        // =========================
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

            Vector3 moveDirWorld =
                (forward * input.z + right * input.x).normalized;

            float baseSpeed = P.Data.GroundData.BaseSpeed;

            bool runAllowed = P.RunHeld && !P.IsCrouchMode;

            // =========================
            // 속도 전이 보간
            // =========================
            float desiredSpeed;

            if (P.IsCrouchTransitioning)
            {
                // 속도 전이용 Lerp Speed (이름 분리)
                float speedLerpSpeed = P.Data.GroundData.ColliderLerpSpeed;

                P.SmoothedMoveSpeed = Mathf.Lerp(
                    P.SmoothedMoveSpeed,
                    P.TargetMoveSpeed,
                    fdt * speedLerpSpeed
                );

                desiredSpeed = P.SmoothedMoveSpeed;
            }
            else
            {
                float modifier = P.IsCrouchMode
                    ? P.Data.GroundData.CrouchWalkSpeedModifier
                    : (runAllowed
                        ? P.Data.GroundData.RunSpeedModifier
                        : P.Data.GroundData.WalkSpeedModifier);

                desiredSpeed = baseSpeed * modifier;
                P.SmoothedMoveSpeed = desiredSpeed;
            }

            // 상태머신에 현재 속도 공유
            SM.SetCurrentSpeed(desiredSpeed);

            horizontalMove = moveDirWorld * desiredSpeed * fdt;

            Vector3 moveDirLocal =
                P.transform.InverseTransformDirection(moveDirWorld);
            moveX = Mathf.Clamp(moveDirLocal.x, -1f, 1f);
            moveY = Mathf.Clamp(moveDirLocal.z, -1f, 1f);
        }

        // =========================
        // 이동 적용
        // =========================
        P.Controller.Move(horizontalMove + verticalMove);

        P.Animator.SetFloat(
            P.AnimationData.MoveXParameterHash,
            moveX,
            MoveDampTime,
            fdt
        );
        P.Animator.SetFloat(
            P.AnimationData.MoveYParameterHash,
            moveY,
            MoveDampTime,
            fdt
        );

        // =========================
        // 캡슐 보정
        // =========================
        float targetHeight = P.IsCrouchMode
            ? P.Data.GroundData.CrouchHeight
            : P.Data.GroundData.StandingHeight;

        float targetCenterY = P.IsCrouchMode
            ? P.Data.GroundData.CrouchCenterY
            : P.Data.GroundData.StandingCenterY;

        // 캡슐 보정용 Lerp Speed (이름 분리)
        float colliderLerpSpeed = P.Data.GroundData.ColliderLerpSpeed;
        float t = 1f - Mathf.Exp(-colliderLerpSpeed * fdt);

        P.Controller.height =
            Mathf.Lerp(P.Controller.height, targetHeight, t);

        Vector3 c = P.Controller.center;
        c.y = Mathf.Lerp(c.y, targetCenterY, t);
        P.Controller.center = c;
    }
}
