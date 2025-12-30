using UnityEngine;

public class PrisonerIdleState : BasePrisonerState
{
    public PrisonerIdleState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        // 1. [안전] 이동 애니메이션 끄기
        Anim?.SetBool("IsMoving", false);

        // 2. [수정] NavMeshAgent 안전 처리
        // agent가 있고 & NavMesh 위에 있을 때만 정지시킴
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // [삭제됨] agent.isStopped = true; 
        // -> 이 줄이 에러의 원인이었으므로 지웁니다.

        // 3. [안전] 수상함 애니메이션 처리
        // Controller나 Anim이 null일 경우를 대비해 ?. 연산자 사용
        if (Controller != null)
        {
            bool isSus = Controller.IsSuspicious;
            Anim?.SetBool("Suspicious", isSus);
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // ...
    }
}