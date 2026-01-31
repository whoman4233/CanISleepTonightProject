using UnityEngine;
using UnityEngine.AI;

public abstract class BasePrisonerState : IPrisonerState
{
    protected PrisonerFSM fsm;
    protected PrisonerController controller; // [변경] Actor -> Controller
    protected Animator anim;
    protected NavMeshAgent agent;
    protected Transform player;

    public BasePrisonerState(PrisonerFSM fsm)
    {
        this.fsm = fsm;
        // 생성자 시점에는 아직 Controller가 연결 안 되었을 수 있으므로
        // fsm.Controller 프로퍼티를 통해 런타임에 접근하도록 설계하거나,
        // Setup 이후에 접근한다고 가정합니다.

        this.player = GameObject.FindGameObjectWithTag("Player")?.transform; 
        this.anim = fsm.GetComponentInChildren<Animator>();
        this.agent = fsm.GetComponent<UnityEngine.AI.NavMeshAgent>();
        RefreshPlayerReference();
    }
    protected Transform Player => fsm.PlayerTransform;

    // 편의를 위한 프로퍼티 (매번 fsm.Controller 쓰기 귀찮으므로)
    protected PrisonerController Controller => fsm.Controller;
    protected Animator Anim => fsm.Anim;
    protected NavMeshAgent Agent => fsm.Agent;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
    public abstract void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir);
    protected void RefreshPlayerReference()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
}