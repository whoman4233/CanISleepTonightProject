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

        // [수정] 데이터(Controller.AIType)만 믿지 말고, 현재 행동 상태(ActionState)가 시끄러운 타입인지도 확인
        PrisonerAIType myType = Controller.AIType;
        bool isNoisyAction = false;

        // ActionState에 접근하여 현재 설정된 타입 확인 (형변환 필요 없이 ActionState가 public이므로 접근 가능)
        if (ActionState != null)
        {
            // ActionState 내부에 현재 타입을 반환하는 Getter가 없으므로
            // myType을 우선 신뢰하되, 아래 리스트에 포함되어 있다면 확실히 리턴시킴
        }

        // ★ 점호 무시 리스트 (확인 사살용 로그 추가)
        if (myType == PrisonerAIType.Ambusher ||
            myType == PrisonerAIType.Singing ||
            myType == PrisonerAIType.Screaming ||
            myType == PrisonerAIType.Crying ||
            myType == PrisonerAIType.Mumbling ||
            myType == PrisonerAIType.HammeringWall ||
            myType == PrisonerAIType.Deadlift)
        {
            Debug.Log($"[FSM] {name} ({myType}) : 점호 무시! 행동 계속함.");
            return;
        }

        switch (myType)
        {
            case PrisonerAIType.Good:
            case PrisonerAIType.Bad:
                ChangeState(InspectionState);
                break;

            case PrisonerAIType.Escaper:
                Debug.Log($"[FSM] {name} 탈주 시작!");
                // if (EscapeState != null) ChangeState(EscapeState);
                break;

            default:
                Debug.Log($"[FSM] {name} ({myType}) 점호 반응 없음 (Default)");
                break;
        }
    }

    public void BackToRoutine()
    {
        // ★ [핵심 수정] 사망 상태(DeadState)라면 복귀 루틴을 실행하지 않음
        if (_currentState == DeadState)
        {
            Debug.Log($"[FSM] {name}는 사망 상태이므로 BackToRoutine을 무시합니다.");
            return;
        }

        // 기존 로직 수행
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