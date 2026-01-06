using UnityEngine;

public class PrisonerDiggingState : BasePrisonerState
{
    public PrisonerDiggingState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 바닥에 웅크려 파는 애니메이션
        Anim.SetBool("IsDigging", true);
        Debug.Log($"[AI] {Controller.name}: (숟가락으로 땅을 파는 중)");
    }

    public override void Exit()
    {
        Anim.SetBool("IsDigging", false);
        base.Exit();
    }
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir) => fsm.ChangeState(fsm.CowerState);
}