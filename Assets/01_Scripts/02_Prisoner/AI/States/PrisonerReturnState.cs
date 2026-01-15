using UnityEngine;

public class PrisonerReturnState : BasePrisonerState
{
    public PrisonerReturnState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        // 목표 지점 (침대)
        Transform target = null;
        if (Controller.AssignedCell != null)
        {
            target = Controller.AssignedCell.prisonerSpawn;
        }

        // 갈 곳이 없으면 바로 행동 상태로 토스
        if (target == null)
        {
            fsm.ChangeState(fsm.ActionState);
            return;
        }

        // 거리 확인 후 이동 시작
        float dist = Vector3.Distance(fsm.transform.position, target.position);
        if (dist > 0.5f)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            anim.SetBool("Walk", true);
            // Debug.Log($"[Return] {Controller.Data.ID} 복귀 시작. 거리: {dist:F1}");
        }
        else
        {
            // 이미 근처면 바로 전환
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Update()
    {
        // 이동 완료 체크
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Exit()
    {
        anim.SetBool("Walk", false);
        if (agent.isOnNavMesh) agent.isStopped = true;
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 복귀 중 맞으면 반응
        if (IsAggressive())
            fsm.ChangeState(fsm.CombatState);
        else
            fsm.ChangeState(fsm.CowerState);
    }

    private bool IsAggressive()
    {
        var type = Controller.AIType;
        return type == PrisonerAIType.HammeringWall ||
               type == PrisonerAIType.Ambusher ||
               type == PrisonerAIType.Escaper ||
               type == PrisonerAIType.Bad;
    }
}