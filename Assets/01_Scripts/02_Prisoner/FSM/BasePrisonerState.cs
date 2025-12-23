using UnityEngine;

public abstract class BasePrisonerState : IPrisonerState
{
    protected PrisonerFSM fsm;
    protected PrisonerActor actor;
    protected Animator anim;
    protected UnityEngine.AI.NavMeshAgent agent;
    protected Transform player;

    public BasePrisonerState(PrisonerFSM fsm)
    {
        this.fsm = fsm;
        this.actor = fsm.GetComponent<PrisonerActor>();
        this.anim = fsm.GetComponentInChildren<Animator>();
        this.agent = fsm.GetComponent<UnityEngine.AI.NavMeshAgent>();
        this.player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
    public abstract void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir);
}