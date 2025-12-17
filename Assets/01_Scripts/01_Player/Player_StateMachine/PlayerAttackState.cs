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

        // 공격은 Attack Layer에서 알아서 Empty로 돌아가니까,
        // “게임플레이 로직상” 잠깐만 락 걸고 Locomotion으로 복귀
        if (_timer >= AttackLockTime)
            SM.ChangeState(SM.Locomotion);
    }
}