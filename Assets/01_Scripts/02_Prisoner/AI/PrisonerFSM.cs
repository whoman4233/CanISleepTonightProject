using UnityEngine;
using UnityEngine.AI;

public class PrisonerFSM : MonoBehaviour
{
    [Header("Points")]
    public Transform InspectionPoint;

    // 외부 컴포넌트 참조
    public PrisonerController Controller { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator Anim { get; private set; }

    private IPrisonerState _currentState;

    // ================================================================
    // [상태 정의] 
    // ================================================================

    // 일반 행동 상태
    public PrisonerActionIdleState ActionState { get; private set; }

    // 기습(매복) 상태
    public IPrisonerState AmbushState { get; private set; }

    // 비주얼 아이들 상태 (특수 외형 전용)
    public PrisonerVisualIdleState VisualIdleState { get; private set; }

    // 특수 로직 상태
    public IPrisonerState CombatState { get; private set; }
    public IPrisonerState CowerState { get; private set; }
    public IPrisonerState DeadState { get; private set; }
    public IPrisonerState InspectionState { get; private set; }
    public IPrisonerState ReturnState { get; private set; }
    public IPrisonerState CenterIdleState { get; private set; }

    // 무적 상태 판정
    public bool IsInvulnerable => _currentState == InspectionState || _currentState == DeadState;

    private void Awake()
    {
        // 상태 객체 생성
        ActionState = new PrisonerActionIdleState(this);
        AmbushState = new PrisonerAmbushState(this);
        VisualIdleState = new PrisonerVisualIdleState(this);

        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);
        InspectionState = new PrisonerInspectionState(this);
        ReturnState = new PrisonerReturnState(this);
        CenterIdleState = new PrisonerCenterIdleState(this);
    }

    public void Setup(PrisonerController controller, NavMeshAgent agent, Animator anim)
    {
        this.Controller = controller;
        this.Agent = agent;
        this.Anim = anim;

        if (controller.AssignedCell != null)
        {
            this.InspectionPoint = controller.AssignedCell.inspectionPoint;
        }

        // 초기 상태 설정 (InitializeBehavior에서 재설정됨)
        ActionState.SetActionType(PrisonerAIType.Good);
        ChangeState(ActionState);
    }

    public void InitializeBehavior(PrisonerAIType aiType)
    {
        // ============================================================
        // ★ [수정] 달리기 스타일 결정 (AIType 기반)
        // ============================================================
        float runStyleValue = 0f; // 기본값 (0: 일반 달리기)

        // ★ 특수 달리기를 사용하는 AI 타입을 여기서 검사합니다.
        // (예: Escaper 등 특정 행동 타입일 때 1번 모션 사용)
        // [원하는 타입으로 if문 조건을 수정하세요]
        if (aiType == PrisonerAIType.Escaper)
        {
            runStyleValue = 1f; // 특수 달리기 (1: 이상한 런)
            Debug.Log($"[FSM Init] {name} ({aiType}) -> 특수 달리기 모션 적용");
        }

        // 애니메이터에 RunStyle 전달
        Anim.SetFloat("RunStyle", runStyleValue);


        // ============================================================
        // 1. 특수 외형(VisualAnomalyType) 체크 및 상태 전환
        // ============================================================
        if (CheckAndEnterVisualState())
        {
            Debug.Log($"[FSM Init] {name} initialized behavior: VisualIdleState");
            return;
        }

        // ============================================================
        // 2. 매복자(Ambusher) 및 일반 타입 처리
        // ============================================================
        if (aiType == PrisonerAIType.Ambusher)
        {
            Debug.Log($"[FSM Init] {name} is Ambusher -> Enter AmbushState");
            ChangeState(AmbushState);
        }
        else
        {
            // 3. 일반 행동 타입 처리
            ActionState.SetActionType(aiType);

            // 이미 ActionState 상태라면 강제로 재진입하여 로직 갱신
            if (_currentState == ActionState)
            {
                ActionState.Exit();
                ActionState.Enter();
                Debug.Log($"[FSM Init] {name} Refreshed ActionState for {aiType}");
            }
            else
            {
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
        if (Anim != null)
        {
            int randomHit = Random.Range(0, 4);
            Anim.SetInteger("HitVariant", randomHit);
        }

        _currentState?.OnDamaged(dmg, hitPoint, hitDir);
    }

    public void OnStartInspection()
    {
        if (Controller == null) return;

        // 현재 상태가 VisualIdleState라면 해당 상태의 로직 위임
        if (_currentState == VisualIdleState)
        {
            // VisualIdleState 내부에 OnStartInspection 구현이 필요함 (형변환 호출)
            ((PrisonerVisualIdleState)VisualIdleState).OnStartInspection();
            return;
        }

        PrisonerAIType myType = Controller.AIType;

        // 점호 무시 리스트 확인
        if (IsIgnoreInspectionType(myType))
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
        // 사망 상태라면 복귀 루틴 무시
        if (_currentState == DeadState)
        {
            Debug.Log($"[FSM] {name}는 사망 상태이므로 BackToRoutine을 무시합니다.");
            return;
        }

        // 특수 외형(비키니, 프랭크 등)은 다시 VisualIdleState로 복귀
        if (_currentState == InspectionState && IsVisualIdleTarget(GetMyVisualType()))
        {
            ChangeState(VisualIdleState);
            return;
        }

        // 기존 로직 수행 (중앙 스폰 vs 일반 복귀)
        if (IsCenterSpawnType())
        {
            ChangeState(CenterIdleState);
        }
        else
        {
            ChangeState(ReturnState);
        }
    }

    // ================================================================
    // Helper Methods
    // ================================================================

    private bool CheckAndEnterVisualState()
    {
        VisualAnomalyType myVisual = GetMyVisualType();
        if (IsVisualIdleTarget(myVisual))
        {
            ChangeState(VisualIdleState);
            return true;
        }
        return false;
    }

    private VisualAnomalyType GetMyVisualType()
    {
        if (PrisonerScheduleManager.Instance != null && Controller != null && Controller.AssignedCell != null)
        {
            return PrisonerScheduleManager.Instance.GetDailyRole(Controller.AssignedCell.cellId).visualType;
        }
        return VisualAnomalyType.None;
    }

    private bool IsVisualIdleTarget(VisualAnomalyType type)
    {
        // None이 아니면 모두 특수 Visual 상태로 간주
        return type != VisualAnomalyType.None;
    }

    private bool IsIgnoreInspectionType(PrisonerAIType type)
    {
        return type == PrisonerAIType.Ambusher ||
               type == PrisonerAIType.Singing ||
               type == PrisonerAIType.Screaming ||
               type == PrisonerAIType.Crying ||
               type == PrisonerAIType.Mumbling ||
               type == PrisonerAIType.HammeringWall ||
               type == PrisonerAIType.Deadlift;
    }

    private bool IsCenterSpawnType()
    {
        VisualAnomalyType type = GetMyVisualType();

        // 프랭크 및 용의자 그룹 확인
        string typeStr = type.ToString();
        return typeStr.StartsWith("PSN_Franke") || typeStr.StartsWith("Suspect");
    }
}