using UnityEngine;

public sealed class PlayerAttackState : PlayerState
{
    // 공격 입력 연타 방지
    private const float AttackLockTime = 0.7f;
    private float _timer;

    public PlayerAttackState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        _timer = 0f;
        P.Animator.SetTrigger(P.AnimationData.AttackParameterHash);
    }

    public override void Tick(float dt)
    {
        _timer += dt;

        if (_timer >= AttackLockTime)
            SM.ChangeState(SM.Locomotion);
    }
    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        // 공격 중 이동을 허용(원하면 속도만 줄이기)
        Vector3 verticalMove = Vector3.zero;
        if (P.ForceReceiver != null)
            verticalMove = P.ForceReceiver.ConsumeMove(fdt, IsGrounded);

        Vector3 horizontalMove = Vector3.zero;

        Vector3 input = new Vector3(P.MoveInput.x, 0f, P.MoveInput.y);
        const float InputDeadZoneSqr = 0.0001f;

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

            float baseSpeed = P.Data.GroundData.BaseSpeed;
            float modifier = P.RunHeld ? P.Data.GroundData.RunSpeedModifier : P.Data.GroundData.WalkSpeedModifier;

            const float AttackMoveMultiplier = 0.6f; // 공격 중 이동감
            float moveSpeed = baseSpeed * modifier * AttackMoveMultiplier;

            horizontalMove = moveDir * moveSpeed * fdt;
        }

        P.Controller.Move(horizontalMove + verticalMove);
    }

    public override void Exit()
    {
        // 상태를 나갈 때 무조건 공격 판정을 종료시킴 (안전장치)
        // 플레이어가 피격되어 상태가 강제 종료되더라도 스윙이 꼬이지 않게 함
        P.WeaponHandler.SetHitColliderEnabled(false);
    }
}