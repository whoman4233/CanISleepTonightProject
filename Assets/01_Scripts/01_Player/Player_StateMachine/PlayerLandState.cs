using UnityEngine;

public sealed class PlayerLandState : PlayerState
{
    private const float MinLandTime = 0.05f; // 너무 즉시 끊기는 것 방지(필요 없으면 0)
    private float _timer;

    public PlayerLandState(PlayerStateMachine sm) : base(sm) { }

    public override void Enter()
    {
        _timer = 0f;

        // 착지 중엔 낙하 종료
        P.Animator.SetBool(P.AnimationData.IsFallingParameterHash, false);

        // Land 애니 시작
        P.Animator.SetTrigger(P.AnimationData.LandParameterHash);
    }

    public override void HandleInput()
    {
        // 조작 잠금(Jump/Attack/Move 무시)
    }

    public override void Tick(float dt)
    {
        _timer += dt;

        AnimatorStateInfo s = P.Animator.GetCurrentAnimatorStateInfo(0);

        // Land 애니가 끝나면 Locomotion으로
        // 여기서 "normalizedTime >= 1"은 클립이 1회 끝났다는 뜻
        if (_timer >= MinLandTime && s.normalizedTime >= 1f)
        {
            P.JumpLocked = false;
            SM.ChangeState(SM.Locomotion);
        }
    }

    public override void FixedTick(float fdt)
    {
        if (P.Controller == null) return;

        // 착지 중에도 ForceReceiver 잔여 수직값 정리(있으면)
        Vector3 verticalMove = Vector3.zero;
        if (P.ForceReceiver != null)
            verticalMove = P.ForceReceiver.ConsumeMove(fdt, true);

        // 수평 이동 0 (조작 잠금)
        P.Controller.Move(verticalMove);
    }
}