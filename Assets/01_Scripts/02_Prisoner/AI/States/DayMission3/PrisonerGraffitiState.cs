using UnityEngine;

public class PrisonerGraffitiState : BasePrisonerState
{
    public PrisonerGraffitiState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 벽을 보고 락카칠 하거나 끄적거리는 애니메이션
        Anim.SetBool("IsGraffiti", true);
        Debug.Log($"[AI] {Controller.name}: (벽에 이상한 그림을 그리는 중)");
    }

    public override void Exit()
    {
        Anim.SetBool("IsGraffiti", false);
        base.Exit();
    }

    // 공격받으면 전투 태세
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir) => fsm.ChangeState(fsm.CombatState);
}