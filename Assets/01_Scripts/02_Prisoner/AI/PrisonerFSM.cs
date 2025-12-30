using UnityEngine;
using UnityEngine.AI;

public class PrisonerFSM : MonoBehaviour
{
    [Header("Points")]
    public Transform InspectionPoint;

    // [변경] 외부에서 주입받을 컴포넌트들
    public PrisonerController Controller { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator Anim { get; private set; }

    private IPrisonerState _currentState;

    // 상태 객체들
    public PrisonerIdleState IdleState { get; private set; }
    public PrisonerInspectionState InspectionState { get; private set; }
    public PrisonerCombatState CombatState { get; private set; }
    public PrisonerCowerState CowerState { get; private set; }
    public PrisonerDeadState DeadState { get; private set; }

    public bool IsInvulnerable => _currentState is PrisonerIdleState;

    private void Awake()
    {
        // 상태 객체 생성 (여기서는 this만 넘기고, 실제 컴포넌트 접근은 프로퍼티로)
        IdleState = new PrisonerIdleState(this);
        InspectionState = new PrisonerInspectionState(this);
        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);
    }

    // [핵심] Controller에서 호출하는 초기화 함수
    public void Setup(PrisonerController controller, NavMeshAgent agent, Animator anim)
    {
        this.Controller = controller;
        this.Agent = agent;
        this.Anim = anim;

        // 점검 위치도 Controller가 알고 있는 Cell 정보에서 가져옴
        if (controller.AssignedCell != null)
        {
            this.InspectionPoint = controller.AssignedCell.inspectionPoint;
        }

        ChangeState(IdleState);
    }

    private void Update() => _currentState?.Update();

    public void ChangeState(IPrisonerState newState)
    {
        if (_currentState == newState) return;
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void OnDamaged(int dmg, Vector3 hitPoint, Vector3 hitDir)
    {
        _currentState?.OnDamaged(dmg, hitPoint, hitDir);
    }
}