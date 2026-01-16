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
    // ================================================================

    // 통합된 일반 행동 상태
    public PrisonerActionIdleState ActionState { get; private set; }

    // ★ [추가] 기습(매복) 상태
    public IPrisonerState AmbushState { get; private set; }

    // [특수 로직]
    public IPrisonerState CombatState { get; private set; }
    public IPrisonerState CowerState { get; private set; }
    public IPrisonerState DeadState { get; private set; }
    public IPrisonerState InspectionState { get; private set; }
    public IPrisonerState ReturnState { get; private set; }
    public IPrisonerState CenterIdleState { get; private set; }

    // (참고) 무적 상태 판정
    public bool IsInvulnerable => _currentState == InspectionState || _currentState == DeadState;

    private void Awake()
    {
        // 상태 객체 생성
        ActionState = new PrisonerActionIdleState(this);

        // ★ [추가] AmbushState 생성
        AmbushState = new PrisonerAmbushState(this);

        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);
        InspectionState = new PrisonerInspectionState(this);
        ReturnState = new PrisonerReturnState(this);
        CenterIdleState = new PrisonerCenterIdleState(this);
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

        // 초기 상태는 ActionState (Good)로 시작 (이후 InitializeBehavior에서 덮어씌워짐)
        ActionState.SetActionType(PrisonerAIType.Good);
        ChangeState(ActionState);
    }

    public void InitializeBehavior(PrisonerAIType aiType)
    {
        // ============================================================
        // ★ [핵심 수정] Ambusher 타입이면 즉시 기습(매복) 상태로 진입
        // ============================================================

        if (aiType == PrisonerAIType.Ambusher)
        {
            Debug.Log($"[FSM Init] {name} is Ambusher -> Enter AmbushState");
            ChangeState(AmbushState);
        }
        else
        {
            // 1. 행동 타입 설정
            ActionState.SetActionType(aiType);

            // 2. [수정] 이미 ActionState 상태라면 ChangeState가 무시되므로, 강제로 재시작
            if (_currentState == ActionState)
            {
                // 강제로 나갔다 들어오게 하여 Enter() 내부의 로그와 소리를 실행시킴
                ActionState.Exit();
                ActionState.Enter();

                Debug.Log($"[FSM Init] {name} Refreshed ActionState for {aiType}");
            }
            else
            {
                // 다른 상태(예: 초기화 전 null 등)였다면 정상적으로 변경
                ChangeState(ActionState);
                Debug.Log($"[FSM Init] {name} initialized behavior: {aiType} -> ActionState");
            }
        }
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

        // ★ [추가] 기습형(Ambusher)은 점호 신호를 무시하고 계속 숨어있어야 함
        // (문이 열리는 순간이 기습 타이밍이므로 InspectionState로 가면 안 됨)
        if (myType == PrisonerAIType.Ambusher)
        {
            Debug.Log($"[FSM] {name} (Ambusher)는 점호 요청을 무시하고 기습 대기합니다.");
            return;
        }

        switch (myType)
        {
            // 1. 일반 죄수들: 점호 받으러 나감
            case PrisonerAIType.Good:
            case PrisonerAIType.Bad:
                ChangeState(InspectionState);
                break;

            // 2. 탈주형(Run): 문 열리면 탈주
            case PrisonerAIType.Escaper:
                Debug.Log($"[FSM] {name} ({myType}) 탈주 시작!");
                // if (EscapeState != null) ChangeState(EscapeState);
                break;

            // 3. 그 외 특이 케이스
            default:
                Debug.Log($"[FSM] {name} ({myType})는 점호 요청을 무시합니다.");
                break;
        }
    }

    public void BackToRoutine()
    {
        if (IsCenterSpawnType())
        {
            ChangeState(CenterIdleState);
        }
        else
        {
            ChangeState(ReturnState);
        }
    }

    // 중앙 스폰 타입인지 확인하는 헬퍼
    private bool IsCenterSpawnType()
    {
        if (PrisonerScheduleManager.Instance == null) return false;
        if (Controller == null || Controller.Data == null) return false;

        var role = PrisonerScheduleManager.Instance.GetDailyRole(Controller.Data.CellID);
        var type = role.visualType;

        return type == VisualAnomalyType.Imposter_Guard ||
               type == VisualAnomalyType.Imposter_NoBeard ||
               type == VisualAnomalyType.Imposter_Earring ||
               type == VisualAnomalyType.Suspect1 ||
               type == VisualAnomalyType.Suspect2 ||
               type == VisualAnomalyType.Suspect3;
    }
}