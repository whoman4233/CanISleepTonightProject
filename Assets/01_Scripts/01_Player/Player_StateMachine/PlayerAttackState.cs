using UnityEngine;

public sealed class PlayerAttackState : PlayerState
{
    // 공격 입력 연타 방지(원하면 PlayerSO로)
    private const float AttackLockTime = 0.15f;
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
}