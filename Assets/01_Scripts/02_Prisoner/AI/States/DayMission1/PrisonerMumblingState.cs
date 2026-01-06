using UnityEngine;

public class PrisonerMumblingState : BasePrisonerState
{
    public PrisonerMumblingState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 침대 위치로 이동 로직이 필요하다면 여기서 NavMeshAgent 설정
        // 예: Agent.SetDestination(Controller.MyBedPosition);

        // 웅크리고 중얼거리는 애니메이션
        Anim.SetBool("IsMumbling", true);

        // 중얼거리는 소리 재생
        // Controller.Sfx.PlayMumblingLoop();
    }

    public override void Exit()
    {
        Anim.SetBool("IsMumbling", false);
        // Controller.Sfx.StopSound();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 이미 웅크리고 있으므로 상태 유지하거나 비명 지르기
        fsm.ChangeState(fsm.CowerState);
    }
}