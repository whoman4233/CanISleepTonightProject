using UnityEngine;

public class PrisonerCryingState : BasePrisonerState
{
    public PrisonerCryingState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        Anim.SetBool("IsCrying", true);

        // 우는 소리 재생
        // Controller.Sfx.PlayCryingLoop();
        Debug.Log($"{Controller.name}: 흑흑.. 잘못했어요..");
    }

    public override void Exit()
    {
        Anim.SetBool("IsCrying", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 이미 멘탈이 나간 상태 -> 계속 움 (Cower 상태와 유사)
        // 여기선 상태 변화 없이 피격 애니메이션만 재생하거나 Cower로 확실히 전환
        fsm.ChangeState(fsm.CowerState);
    }
}