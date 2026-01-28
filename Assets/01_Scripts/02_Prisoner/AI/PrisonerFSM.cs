using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

public class PrisonerFSM : MonoBehaviour
{
    [Header("Points")]
    public Transform InspectionPoint;

    [Header("QTE Settings")]
    [SerializeField] private QTEActionSO defaultQteAction;
    // ★ [추가] QTE 접근 시 멈출 거리 (1.0 ~ 1.5 정도 추천)
    [field: SerializeField] public float QteStopDistance { get; private set; } = 1.2f;

    [Header("Ambush Settings")]
    [SerializeField] private float ambushDelay = 1.5f;
    private Coroutine _ambushCoroutine;

    // 외부 컴포넌트 참조
    public PrisonerController Controller { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator Anim { get; private set; }

    public IPrisonerState _currentState;

    // ================================================================
    // [이벤트 핸들러 캐시]
    // ================================================================
    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;

    // ================================================================
    // [상태 정의] 
    // ================================================================
    public PrisonerActionIdleState ActionState { get; private set; }
    public IPrisonerState AmbushState { get; private set; }
    public PrisonerVisualIdleState VisualIdleState { get; private set; }
    public PrisonerBikiniState BikiniState { get; private set; }
    public IPrisonerState CombatState { get; private set; }
    public IPrisonerState CowerState { get; private set; }
    public IPrisonerState DeadState { get; private set; }
    public IPrisonerState InspectionState { get; private set; }
    public IPrisonerState ReturnState { get; private set; }
    public IPrisonerState CenterIdleState { get; private set; }
    public IPrisonerState QTEApproachState { get; private set; }

    // ★ [추가] 탈주 상태 프로퍼티
    public IPrisonerState EscapeState { get; private set; }

    public bool IsInvulnerable => _currentState == InspectionState || _currentState == DeadState;

    private void Awake()
    {
        // 상태 객체 생성
        ActionState = new PrisonerActionIdleState(this);
        AmbushState = new PrisonerAmbushState(this);
        VisualIdleState = new PrisonerVisualIdleState(this);
        BikiniState = new PrisonerBikiniState(this);
        CombatState = new PrisonerCombatState(this);
        CowerState = new PrisonerCowerState(this);
        DeadState = new PrisonerDeadState(this);
        InspectionState = new PrisonerInspectionState(this);
        ReturnState = new PrisonerReturnState(this);
        CenterIdleState = new PrisonerCenterIdleState(this);

        // ★ [추가] EscapeState 생성
        EscapeState = new PrisonerEscapeState(this);

        if (defaultQteAction == null)
            Debug.LogWarning($"[PrisonerFSM] {name} : QTE Action Data is missing in Inspector!");

        QTEApproachState = new PrisonerQTEApproachState(this, defaultQteAction);

        // 이벤트 변수 초기화
        _onInspectionStarted = OnInspectionStarted;
        _onInspectionEnded = OnInspectionEnded;
    }

    private void OnEnable()
    {
        if (_onInspectionStarted != null) EventBus.Subscribe(_onInspectionStarted);
        if (_onInspectionEnded != null) EventBus.Subscribe(_onInspectionEnded);
    }

    private void OnDisable()
    {
        if (_onInspectionStarted != null) EventBus.Unsubscribe(_onInspectionStarted);
        if (_onInspectionEnded != null) EventBus.Unsubscribe(_onInspectionEnded);
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

        ActionState.SetActionType(PrisonerAIType.Good);
        ChangeState(ActionState);
    }

    public void InitializeBehavior(PrisonerAIType aiType)
    {
        Anim.SetBool("IsntStanding", false);
        float runStyleValue = (aiType == PrisonerAIType.Escaper) ? 1f : 0f;
        Anim.SetFloat("RunStyle", runStyleValue);

        if (CheckAndEnterVisualState()) return;

        // QTE_Attacker가 시작하자마자 달려들던 if문 삭제됨.
        // 이제 아래의 else 블록으로 넘어가서 평소(ActionState)에는 얌전히 대기합니다.

        // 매복자 처리
        if (aiType == PrisonerAIType.Ambusher)
        {
            // 상태 진입 전, 컨트롤러에게 "암살자 무기(단검) 들어!"라고 명령
            Controller.StartActionBehavior(PrisonerAIType.Ambusher);
            ChangeState(AmbushState);
        }
        else
        {
            // 일반 AI 및 QTE_Attacker 처리
            // QTE_Attacker도 ActionState(기본 대기)로 시작하며, 점호 이벤트가 오면 OnInspectionStarted에서 반응합니다.
            ActionState.SetActionType(aiType);
            if (_currentState == ActionState)
            {
                ActionState.Exit();
                ActionState.Enter();
            }
            else
            {
                ChangeState(ActionState);
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
            int randomHit = UnityEngine.Random.Range(0, 4);
            Anim.SetFloat("HitVariant", (float)randomHit);
        }
        _currentState?.OnDamaged(dmg, hitPoint, hitDir);
    }

    public void OnStartInspection()
    {
        if (Controller == null) return;
        if (_currentState == DeadState) return;
        if (_currentState == BikiniState) return;
        if (_currentState == VisualIdleState)
        {
            ((PrisonerVisualIdleState)VisualIdleState).OnStartInspection();
            return;
        }

        PrisonerAIType myType = Controller.AIType;

        // 점호 무시 타입인지 확인
        if (IsIgnoreInspectionType(myType)) return;

        switch (myType)
        {
            case PrisonerAIType.Good:
            case PrisonerAIType.Bad:
            case PrisonerAIType.QTE_Attacker: //  QTE 공격자도 일단 점호 태세를 취함
                ChangeState(InspectionState);
                break;
            case PrisonerAIType.Escaper:
                Debug.Log($"[FSM] {name} 탈주 시작!");
                //  로그만 찍지 말고 실제로 상태 전환
                ChangeState(EscapeState);
                break;
        }
    }

    public void BackToRoutine()
    {
        if (_currentState == DeadState) return;

        //  QTE 접근 중이거나 전투 중일 때는 루틴 복귀(점호 종료 등) 명령을 무시
        if (_currentState == QTEApproachState || _currentState == CombatState) return;

        // 비키니 등 특수 비주얼 상태 복귀
        if (GetMyVisualType() == VisualAnomalyType.BikiniModel)
        {
            ChangeState(BikiniState);
            return;
        }
        // 특수 비주얼(프랭크 등) 복귀
        if (_currentState == InspectionState && IsVisualIdleTarget(GetMyVisualType()))
        {
            ChangeState(VisualIdleState);
            return;
        }

        if (IsCenterSpawnType()) ChangeState(CenterIdleState);
        else ChangeState(ReturnState);
    }

    // ================================================================
    // 기습 공격 로직
    // ================================================================

    private void OnInspectionStarted(InspectionStartedEvent evt)
    {
        if (_currentState == DeadState || _currentState == CowerState || _currentState == CombatState)
            return;

        if (!IsTargetRelatedToMe(evt.Target))
            return;

        // ★ [핵심 유지] 내 AI 타입이 "QTE 공격자"가 아니면 기습 로직을 아예 실행하지 않음
        if (Controller.AIType != PrisonerAIType.QTE_Attacker)
            return;

        if (_ambushCoroutine != null) StopCoroutine(_ambushCoroutine);
        _ambushCoroutine = StartCoroutine(CoWaitAndAmbush());
    }

    private void OnInspectionEnded(InspectionEndedEvent evt)
    {
        if (_ambushCoroutine != null)
        {
            StopCoroutine(_ambushCoroutine);
            _ambushCoroutine = null;
        }

        // (추가 안전장치) 만약 이미 QTE를 하러 가는 중이라면 점호 끝났다고 돌아가지 않음
        if (_currentState == QTEApproachState) return;
    }

    private IEnumerator CoWaitAndAmbush()
    {
        yield return new WaitForSeconds(ambushDelay);
        Debug.Log($"[FSM] {name} : 기습 공격 시작!");
        ChangeState(QTEApproachState);
        _ambushCoroutine = null;
    }

    private bool IsTargetRelatedToMe(IInspectable target)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) return false;
        if (targetMono.gameObject == this.gameObject) return true;

        float distance = Vector3.Distance(transform.position, targetMono.transform.position);
        if (distance < 4.0f) return true;

        return false;
    }

    // ================================================================
    // Helper Methods
    // ================================================================

    private bool CheckAndEnterVisualState()
    {
        VisualAnomalyType myVisual = GetMyVisualType();
        if (myVisual == VisualAnomalyType.BikiniModel)
        {
            ChangeState(BikiniState);
            return true;
        }
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
        if (type == VisualAnomalyType.None || type == VisualAnomalyType.BikiniModel)
            return false;

        string typeStr = type.ToString();
        if (typeStr.Contains("Muscular") ||
            typeStr.Contains("Nervous") ||
            typeStr.Contains("Tattooed") ||
            typeStr.Contains("Intelligent"))
        {
            return false;
        }

        return true;
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
        string typeStr = type.ToString();
        return typeStr.StartsWith("PSN_Franke") || typeStr.StartsWith("Suspect");
    }
}