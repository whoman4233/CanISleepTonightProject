using UnityEngine;

public sealed class PlayerJumpState : PlayerState
{
    private const float MinAirTime = 0.05f;
    private const float FallEnterHeightThreshold = 2.5f;

    // 공중 수평 입력 데드존(매직넘버 제거)
    private const float AirInputDeadzoneSqr = 0.01f;

    // 공중 이동 허용 비율(원하면 나중에 PlayerSO로 빼도 됨)
    private const float AirControlMultiplier = 0.8f;

    private static Transform _cachedMainCameraTransform;

    private float _timer;

    public PlayerJumpState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        P.Sfx?.StopFootstepLoopImmediate();
        P.ForceClearCrouchTransitionLock();

        _timer = 0f;

        // 점프로 시작했을 때만 점프 애니/점프 힘 적용
        if (P.AirFromJump)
        {
            P.Animator.SetTrigger(P.AnimationData.JumpParameterHash);
            P.Sfx?.PlayJumpSfx();

            if (P.ForceReceiver != null)
                P.ForceReceiver.SetJumpVelocity(P.Data.JumpData.JumpForce);
        }

        // 공중 시작 높이는 Locomotion에서 세팅됨
        float y = P.transform.position.y;
        P.AirApexY = Mathf.Max(P.AirApexY, y);

        // 점프 중에는 Falling을 기본으로 꺼둔다
        P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, false);
    }

    public override void Tick(float dt)
    {
        _timer += dt;

        // 정점 갱신
        float y = P.transform.position.y;
        if (y > P.AirApexY) P.AirApexY = y;

        bool isInAir = !IsGrounded;
        if (isInAir && P.ForceReceiver != null)
        {
            float fallDistance = P.AirApexY - y;               // 정점부터 얼마나 떨어졌는지
            bool isFallingDown = P.ForceReceiver.VerticalVelocity < 0f;

            // 낙하거리 임계치 넘을 때만 Falling 애니/상태 진입
            if (isFallingDown && fallDistance >= FallEnterHeightThreshold)
            {
                P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, true);
                SM.ChangeState(SM.Fall);
                return;
            }
        }

        // 착지 시 Locomotion 복귀
        if (IsGrounded && _timer >= MinAirTime)
        {
            SM.ChangeState(SM.Locomotion);
        }
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        // 수직(중력/점프) 적용
        Vector3 verticalMove = Vector3.zero;
        if (P.ForceReceiver != null)
            verticalMove = P.ForceReceiver.ConsumeMove(fdt, IsGrounded);

        // 수평(공중 조작) 적용
        Vector3 horizontalMove = Vector3.zero;

        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);
        if (input.sqrMagnitude >= AirInputDeadzoneSqr)
        {
            Transform cam = GetMainCameraTransform();
            if (cam != null)
            {
                Vector3 forward = cam.forward;
                Vector3 right = cam.right;
                forward.y = 0f;
                right.y = 0f;

                forward.Normalize();
                right.Normalize();

                Vector3 moveDir = forward * input.z + right * input.x;

                float baseSpeed = P.Data.GroundData.BaseSpeed;
                float modifier = P.RunHeld ? P.Data.GroundData.RunSpeedModifier : P.Data.GroundData.WalkSpeedModifier;
                float moveSpeed = baseSpeed * modifier * AirControlMultiplier;

                horizontalMove = moveDir * moveSpeed * fdt;
            }
        }

        P.Controller.Move(horizontalMove + verticalMove);
    }

    private static Transform GetMainCameraTransform()
    {
        if (_cachedMainCameraTransform != null) return _cachedMainCameraTransform;

        Camera cam = Camera.main;
        _cachedMainCameraTransform = cam != null ? cam.transform : null;
        return _cachedMainCameraTransform;
    }
}