using UnityEngine;

class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    public PrisonerQTEApproachState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        anim.SetBool("Walk", true);
    }

    public override void Update()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            PrisonerQTEContext.SetAttacker(fsm.transform);
            EventBus.Publish(new QTEStartedEvent { Action = qteAction });
        }
    }

    public override void Exit()
    {
        agent.ResetPath();
        anim.SetBool("Walk", false);
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        throw new System.NotImplementedException();
    }
}
