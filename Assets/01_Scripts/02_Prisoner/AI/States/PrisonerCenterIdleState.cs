using UnityEngine;

public class PrisonerCenterIdleState : BasePrisonerState
{
    public PrisonerCenterIdleState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        // 제자리 정지
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        anim.SetBool("Walk", false);

        // 중앙 죄수 전용 애니메이션 트리거가 있다면 여기서 실행
        // anim.SetTrigger("Idle_Center"); 
    }

    public override void Update()
    {
        // 플레이어가 가까이 오면 쳐다보기 (LookAt)
        if (player != null && Vector3.Distance(fsm.transform.position, player.position) < 3.0f)
        {
            Vector3 dir = (player.position - fsm.transform.position).normalized;
            dir.y = 0;
            if (dir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, lookRot, Time.deltaTime * 5f);
            }
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 맞으면 전투 태세 (임포스터 등은 반격할 수도 있음)
        // 성향에 따라 분기 가능
        fsm.ChangeState(fsm.CombatState);
    }
}