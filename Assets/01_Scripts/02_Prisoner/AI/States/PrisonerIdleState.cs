using UnityEngine;

public class PrisonerIdleState : BasePrisonerState
{
    public PrisonerIdleState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        agent.isStopped = true;
        anim.SetBool("IsSitting", true); // 앉아있는 애니메이션 루프
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // Actor에서 차단하므로 여기까지 들어오지 않겠지만, 
        // 만약 들어온다면 여기서 "비웃음"이나 "무시" 애니메이션 처리 가능
    }
}