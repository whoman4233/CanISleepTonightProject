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

        Transform target = null;
        if (Controller.AssignedCell != null)
        {
            target = Controller.AssignedCell.prisonerSpawn;
        }

        if (target == null)
        {
            fsm.ChangeState(fsm.ActionState);
            return;
        }

        float dist = Vector3.Distance(fsm.transform.position, target.position);
        if (dist > 0.5f)
        {
            //NavMesh 위에 있을 때만 이동 명령
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);
                anim.SetBool("Walk", true);
            }
            else
            {
                // NavMesh 위가 아니면 강제로 위치 이동시키거나 바로 상태 전환
                Debug.LogWarning($"[ReturnState] {Controller.name} is not on NavMesh. Force transition.");
                fsm.ChangeState(fsm.ActionState);
            }
        }
        else
        {
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Update()
    {
        // 에이전트가 NavMesh 위에 없으면 거리 계산 시도 금지 (에러 원인)
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled)
        {
            return;
        }

        // 이동 완료 체크
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Exit()
    {
        anim.SetBool("Walk", false);
        // 나갈 때도 안전하게 체크
        if (agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
        }
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