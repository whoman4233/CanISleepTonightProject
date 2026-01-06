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
    public IPrisonerState IdleState { get; private set; }
    public IPrisonerState CombatState { get; private set; }
    public IPrisonerState CowerState { get; private set; }
    public IPrisonerState DeadState { get; private set; }
    public IPrisonerState InspectionState { get; private set; }

    // 🔥 [추가] 특수 행동 상태들
    public IPrisonerState SingingState { get; private set; }
    public IPrisonerState ScreamingState { get; private set; }
    public IPrisonerState MumblingState { get; private set; }
    public IPrisonerState HammeringState { get; private set; }
    public IPrisonerState DeadliftingState { get; private set; }
    public IPrisonerState CryingState { get; private set; }

    public bool IsInvulnerable => _currentState is PrisonerIdleState;

    private void Awake()
    {
        // 상태 객체 생성 (여기서는 this만 넘기고, 실제 컴포넌트 접근은 프로퍼티로)
        IdleState = new PrisonerIdleState(this);
        InspectionState = new PrisonerInspectionState(this);
        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);

        // 🔥 [추가] 특수 상태 생성
        SingingState = new PrisonerSingingState(this);
        ScreamingState = new PrisonerScreamingState(this);
        MumblingState = new PrisonerMumblingState(this);
        HammeringState = new PrisonerHammeringState(this);
        DeadliftingState = new PrisonerDeadliftingState(this);
        CryingState = new PrisonerCryingState(this);
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

    public void InitializeBehavior(PrisonerAIType aiType)
    {
        // 1. 상태 전환 로직
        switch (aiType)
        {
            // [1일차 소음]
            case PrisonerAIType.Singing:
                ChangeState(SingingState);
                break;
            case PrisonerAIType.Screaming:
                ChangeState(ScreamingState);
                break;
            case PrisonerAIType.Mumbling:
                ChangeState(MumblingState);
                break;
            case PrisonerAIType.HammeringWall:
                ChangeState(HammeringState);
                break;
            case PrisonerAIType.Deadlift:
                ChangeState(DeadliftingState);
                break;
            case PrisonerAIType.Crying:
                ChangeState(CryingState);
                break;

            // [기본]
            case PrisonerAIType.Good:
            case PrisonerAIType.Bad:
            default:
                ChangeState(IdleState);
                break;
        }
        Debug.Log($"[FSM Init] {name} initialized with behavior: {aiType}");
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