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
    // [추가] QTE 접근 시 멈출 거리 (1.0 ~ 1.5 정도 추천)
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
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    // ★ [추가] 현재 조사받고 있는 대상을 기억하는 변수 (Struct 수정 없이 필터링하기 위함)
    private IInspectable _cachedTarget;

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

    // [추가] 탈주 상태 프로퍼티
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

        // [추가] EscapeState 생성
        EscapeState = new PrisonerEscapeState(this);

        if (defaultQteAction == null)
            Debug.LogWarning($"[PrisonerFSM] {name} : QTE Action Data is missing in Inspector!");

        QTEApproachState = new PrisonerQTEApproachState(this, defaultQteAction);

        // 이벤트 변수 초기화
        _onInspectionStarted = OnInspectionStarted;
        _onInspectionEnded = OnInspectionEnded;
        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;
    }

    private void OnEnable()
    {
        if (_onInspectionStarted != null) EventBus.Subscribe(_onInspectionStarted);
        if (_onInspectionEnded != null) EventBus.Subscribe(_onInspectionEnded);
        if (_onQTEStarted != null) EventBus.Subscribe(_onQTEStarted);
        if (_onQTEEnded != null) EventBus.Subscribe(_onQTEEnded);
    }

    private void OnDisable()
    {
        if (_onInspectionStarted != null) EventBus.Unsubscribe(_onInspectionStarted);
        if (_onInspectionEnded != null) EventBus.Unsubscribe(_onInspectionEnded);
        if (_onQTEStarted != null) EventBus.Unsubscribe(_onQTEStarted);
        if (_onQTEEnded != null) EventBus.Unsubscribe(_onQTEEnded);
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

        // 매복자 처리
        if (aiType == PrisonerAIType.Ambusher)
        {
            Controller.StartActionBehavior(PrisonerAIType.Ambusher);
            ChangeState(AmbushState);
        }
        else
        {
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

        // [수정] 이미 QTE 접근 중이거나 전투 중이면 점호 명령 무시 (상태 덮어쓰기 방지)
        if (_currentState == QTEApproachState || _currentState == CombatState) return;

        if (_currentState == VisualIdleState)
        {
            ((PrisonerVisualIdleState)VisualIdleState).OnStartInspection();
            return;
        }

        PrisonerAIType myType = Controller.AIType;

        if (IsIgnoreInspectionType(myType)) return;

        switch (myType)
        {
            case PrisonerAIType.Good:
            case PrisonerAIType.Bad:
            case PrisonerAIType.QTE_Attacker: // QTE 공격자도 일단 점호 태세를 취함
                ChangeState(InspectionState);
                break;
            case PrisonerAIType.Escaper:
                Debug.Log($"[FSM] {name} 탈주 시작!");
                ChangeState(EscapeState);
                break;
        }
    }

    public void BackToRoutine()
    {
        if (_currentState == DeadState) return;

        // QTE 접근 중이거나 전투 중일 때는 루틴 복귀 명령 무시
        if (_currentState == QTEApproachState || _currentState == CombatState) return;

        if (GetMyVisualType() == VisualAnomalyType.BikiniModel)
        {
            ChangeState(BikiniState);
            return;
        }

        if (_currentState == InspectionState && IsVisualIdleTarget(GetMyVisualType()))
        {
            ChangeState(VisualIdleState);
            return;
        }

        if (IsCenterSpawnType()) ChangeState(CenterIdleState);
        else ChangeState(ReturnState);
    }

    // ================================================================
    // 기습 공격 및 QTE 로직
    // ================================================================

    private void OnInspectionStarted(InspectionStartedEvent evt)
    {
        if (_currentState == DeadState || _currentState == CowerState || _currentState == CombatState)
            return;

        // 내꺼 아니면 무시 + 캐시 초기화
        if (!IsTargetRelatedToMe(evt.Target))
        {
            _cachedTarget = null;
            return;
        }

        // ★ [핵심] "지금 내 물건을 조사 중이다"라고 기억해둠 (QTE 때 확인용)
        _cachedTarget = evt.Target;

        // 내 AI 타입이 "QTE 공격자"가 아니면 기습 로직을 실행하지 않음
        if (Controller.AIType != PrisonerAIType.QTE_Attacker)
            return;

        if (_ambushCoroutine != null) StopCoroutine(_ambushCoroutine);
        _ambushCoroutine = StartCoroutine(CoWaitAndAmbush());
    }

    // [수정] Struct 수정 없이 논리로 필터링
    private void OnQTEStarted(QTEStartedEvent evt)
    {
        if (_currentState == DeadState) return;

        // 1. 점호 시작 때 기억해둔 타겟이 없다면? -> 내 구역 조사가 아니므로 QTE도 내 것이 아님 -> 무시
        if (_cachedTarget == null) return;

        // (안전장치) 기억해둔 타겟이 정말 내 것인지 한 번 더 체크
        if (!IsTargetRelatedToMe(_cachedTarget)) return;

        // 여기까지 왔으면 "내 물건 조사 중에 QTE가 터진 것"임.
        // 1.5초 대기 코루틴 취소
        if (_ambushCoroutine != null)
        {
            StopCoroutine(_ambushCoroutine);
            _ambushCoroutine = null;
        }

        Debug.Log($"[FSM] {name} : 내 물건({_cachedTarget}) 조사 중 QTE 발생! 덮칩니다.");

        // InspectionState 등을 끊고 즉시 QTE 접근 상태로 전환
        ChangeState(QTEApproachState);
    }

    // [추가] QTE 종료 핸들러
    private void OnQTEEnded(QTEEndedEvent evt)
    {
        // 내가 QTE 진행 중이 아니었다면 무시
        if (_currentState != QTEApproachState) return;
        if (_currentState == DeadState) return;

        // QTE가 끝났으므로(성공/실패 불문) 전투 상태로 전환
        Debug.Log($"[FSM] {name} : QTE 종료됨 -> 전투 모드 전환");
        ChangeState(CombatState);

        // 종료되었으니 타겟 기억 지움
        _cachedTarget = null;
    }

    private void OnInspectionEnded(InspectionEndedEvent evt)
    {
        // 점호가 끝났으니 기억해둔 타겟 정보 초기화
        _cachedTarget = null;

        if (_ambushCoroutine != null)
        {
            StopCoroutine(_ambushCoroutine);
            _ambushCoroutine = null;
        }

        if (_currentState == QTEApproachState) return;
    }

    private IEnumerator CoWaitAndAmbush()
    {
        // 1.5초간 대기 (이 동안은 InspectionState가 유지됨)
        yield return new WaitForSeconds(ambushDelay);

        Debug.Log($"[FSM] {name} : 기습 공격 시작!");
        ChangeState(QTEApproachState);
        _ambushCoroutine = null;
    }

    private bool IsTargetRelatedToMe(IInspectable target)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) return false;

        // 1. 점호 대상이 '나 자신'이면 무조건 True
        if (targetMono.gameObject == this.gameObject) return true;

        // 2. '내 감방(AssignedCell)'을 기준으로 거리 체크
        if (Controller != null && Controller.AssignedCell != null)
        {
            Vector3 cellCenter = Controller.AssignedCell.transform.position;
            Vector3 targetPos = targetMono.transform.position;

            float distanceToCell = Vector3.Distance(cellCenter, targetPos);

            // [수정] 4.0f -> 2.5f 로 축소 (옆방 침범 방지)
            if (distanceToCell < 4f) return true;
        }
        else
        {
            // (예외) 만약 배정된 방이 없는 상태라면, 내 몸 기준으로 체크
            float distToMe = Vector3.Distance(transform.position, targetMono.transform.position);
            if (distToMe < 2.0f) return true;
        }

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