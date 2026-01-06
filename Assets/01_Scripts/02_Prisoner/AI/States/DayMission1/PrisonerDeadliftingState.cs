using UnityEngine;

public class PrisonerDeadliftingState : BasePrisonerState
{
    public PrisonerDeadliftingState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        Anim.SetBool("IsDeadlifting", true);
        // 운동 기합 소리
        // Controller.Sfx.PlayExerciseGrunt();
    }

    public override void Exit()
    {
        Anim.SetBool("IsDeadlifting", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 운동 중 방해받으면 화냄 (전투 상태)
        fsm.ChangeState(fsm.CombatState);
    }
}