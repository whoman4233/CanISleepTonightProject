using UnityEngine;
using UnityEngine.AI;

public class PrisonerFSM : MonoBehaviour
{
    [Header("Points")]
    public Transform InspectionPoint;

    // 외부에서 주입받을 컴포넌트들
    public PrisonerController Controller { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator Anim { get; private set; }

    private IPrisonerState _currentState;

    // ================================================================
    // [상태 정의] 
    // 기존의 잡다한 상태들을 ActionState 하나로 통합했습니다.
    // ================================================================

    // ★ [통합] 대기, 노래, 비명, 땅파기, 기습대기 등 "제자리 행동"을 모두 담당
    public PrisonerActionIdleState ActionState { get; private set; }



    // [특수 로직] 전투, 쫄기, 사망, 점호 등은 별도 로직이므로 유지
    public IPrisonerState CombatState { get; private set; }
    public IPrisonerState CowerState { get; private set; }
    public IPrisonerState DeadState { get; private set; }
    public IPrisonerState InspectionState { get; private set; }

    // (참고) 무적 상태 판정: 점호(Inspection) 중이거나 죽었을 때만 무적으로 설정하는 것이 일반적입니다.
    // 기존 코드대로라면 Idle일 때 무적이라 때릴 수가 없으므로 로직을 수정했습니다.
    public bool IsInvulnerable => _currentState == InspectionState || _currentState == DeadState;

    private void Awake()
    {
        // 상태 객체 생성
        // ★ 통합된 ActionState 하나만 생성하면 됩니다.
        ActionState = new PrisonerActionIdleState(this);

        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);
        InspectionState = new PrisonerInspectionState(this);
    }

    // Controller에서 호출하는 초기화 함수
    public void Setup(PrisonerController controller, NavMeshAgent agent, Animator anim)
    {
        this.Controller = controller;
        this.Agent = agent;
        this.Anim = anim;

        if (controller.AssignedCell != null)
        {
            this.InspectionPoint = controller.AssignedCell.inspectionPoint;
        }

        // 초기 상태는 ActionState (Type 0 = Normal Idle)로 시작
        ActionState.SetActionType(PrisonerAIType.Good);
        ChangeState(ActionState);
    }

    public void InitializeBehavior(PrisonerAIType aiType)
    {
        // ============================================================
        // ★ [핵심 수정] 거대한 Switch문을 제거하고 통합 로직 적용
        // 어떤 타입이든 ActionState에게 "너 이거 해"라고 알려주고 전환합니다.
        // ============================================================

        ActionState.SetActionType(aiType);
        ChangeState(ActionState);

        Debug.Log($"[FSM Init] {name} initialized behavior: {aiType} -> ActionState");
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

    public void OnStartInspection()
    {
        if (Controller == null) return;

        PrisonerAIType myType = Controller.AIType;

        switch (myType)
        {
            // 1. 고정형(Stay), 비키니(Bikini): 
            // 점호 신호를 무시하고 하던 행동(Idle) 계속 유지
            case PrisonerAIType.Good:
            case PrisonerAIType.Bad:
            case PrisonerAIType.Ambusher:
                ChangeState(InspectionState);
                break;

            // 2. 탈주형(Run): 
            // 문이 열리자마자 탈주 시작
            case PrisonerAIType.Escaper:
                Debug.Log($"[FSM] {name} ({myType}) 탈주 시작!");
                // if (EscapeState != null) ChangeState(EscapeState);
                // 지금은 EscapeState가 변수로 선언 안 되어 있을 수 있으니 로그만
                break;

            // 3. 순응형(Good), 반항형(Bad), 기습형(Ambush) 등:
            // 정상적으로 점호 자세(Inspection)로 전환
            default:
                Debug.Log($"[FSM] {name} ({myType})는 점호 요청을 무시합니다.");
                break;
        }
    }
}