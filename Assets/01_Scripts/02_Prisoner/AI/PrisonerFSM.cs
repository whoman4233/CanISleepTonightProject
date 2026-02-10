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
    // QTE 접근 시 멈출 거리
    [field: SerializeField] public float QteStopDistance { get; private set; } = 1.2f;

    [Header("Ambush Settings")]
    [SerializeField] private float ambushDelay = 1.5f;
    private Coroutine _ambushCoroutine;

    // 외부 컴포넌트 참조
    public PrisonerController Controller { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public Animator Anim { get; private set; }

    public IPrisonerState _currentState;
    public IPrisonerState CurrentState => _currentState;

    // ================================================================
    // [이벤트 핸들러 캐시]
    // ================================================================
    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    // 현재 조사받고 있는 대상을 기억하는 변수 (Struct 수정 없이 필터링용)
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
        float runStyleValue = (aiType == PrisonerAIType.Escaping) ? 1f : 0f;
        Anim.SetFloat("RunStyle", runStyleValue);

        if (CheckAndEnterVisualState()) return;

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

        // 이미 QTE 접근 중이거나 전투 중이면 점호 명령 무시
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
            case PrisonerAIType.QTE_Attacker: // QTE 공격자도 일단 점호 태세
                ChangeState(InspectionState);
                break;
            case PrisonerAIType.Escaping:
                Debug.Log($"[FSM] {name} 탈주 시작!");
                ChangeState(EscapeState);
                break;
        }
    }

    public void BackToRoutine()
    {
        if (_currentState == DeadState) return;

        // QTE 접근 중이거나 전투 중일 때는 복귀 명령 무시
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

        // "지금 내 물건을 조사 중이다" 기억
        _cachedTarget = evt.Target;

        if (Controller.AIType != PrisonerAIType.QTE_Attacker)
            return;

        if (_ambushCoroutine != null) StopCoroutine(_ambushCoroutine);
        _ambushCoroutine = StartCoroutine(CoWaitAndAmbush());
    }

    private void OnQTEStarted(QTEStartedEvent evt)
    {
        if (_currentState == DeadState) return;

        // 1. 점호 시작 때 기억해둔 타겟이 없다면? -> 내 구역 QTE 아님 -> 무시
        if (_cachedTarget == null) return;

        // (안전장치)
        if (!IsTargetRelatedToMe(_cachedTarget)) return;

        // "내 물건 조사 중에 QTE 발생"
        // 대기 코루틴 취소 후 즉시 접근
        if (_ambushCoroutine != null)
        {
            StopCoroutine(_ambushCoroutine);
            _ambushCoroutine = null;
        }

        Debug.Log($"[FSM] {name} : QTE 발생! 덮칩니다.");
        ChangeState(QTEApproachState);
    }

    // ★ [수정] 강제 상태 전환 삭제
    private void OnQTEEnded(QTEEndedEvent evt)
    {
        // QTE 종료 시 데이터 정리만 수행
        // (상태 전환은 QTEApproachState.OnResultAnimationFinished가 담당)
        if (_currentState == QTEApproachState)
        {
            _cachedTarget = null;
        }
    }

    private void OnInspectionEnded(InspectionEndedEvent evt)
    {
        _cachedTarget = null;

        // 대기 중이었다면 코루틴 취소 (이건 기존과 동일)
        if (_ambushCoroutine != null)
        {
            StopCoroutine(_ambushCoroutine);
            _ambushCoroutine = null;
        }

        // 접근 중일 때의 처리 로직 추가
        if (_currentState == QTEApproachState)
        {
            // 1. 만약 내가 현재 '공격자'로 등록되어 있다면?
            //    -> QTE가 정상적으로 시작되어 시스템이 상세보기를 끈 상황임.
            //    -> 이때는 멈추면 안 되므로 그냥 리턴(진행).
            if (PrisonerQTEContext.CurrentAttacker == this.gameObject)
                return;

            // 2. 공격자가 아닌데 상세보기가 꺼졌다?
            //    -> 플레이어가 QTE 시작 전에 ESC 등으로 닫고 튄 상황.
            //    -> 추격을 멈추고 다시 점호 대기 상태(InspectionState)로 복귀.
            Debug.Log($"[FSM] {name} : 상세보기가 종료되어 기습을 중단하고 원위치합니다.");
            ChangeState(InspectionState);
        }
    }

    private IEnumerator CoWaitAndAmbush()
    {
        yield return new WaitForSeconds(ambushDelay);

        //1.5초 기다리는 동안 타겟(점호 대상)이 사라졌거나 취소되었는지 확인
        if (_cachedTarget == null)
        {
            Debug.Log($"[FSM] {name} : 기습하려 했으나 대상이 사라짐 (취소)");
            _ambushCoroutine = null;
            yield break;
        }

        Debug.Log($"[FSM] {name} : 기습 공격 시작!");
        ChangeState(QTEApproachState);
        _ambushCoroutine = null;
    }

    private bool IsTargetRelatedToMe(IInspectable target)
    {
        MonoBehaviour targetMono = target as MonoBehaviour;
        if (targetMono == null) return false;

        // 1. 점호 대상이 '나 자신'
        if (targetMono.gameObject == this.gameObject) return true;

        // 2. '내 감방(AssignedCell)' 기준 거리 체크
        if (Controller != null && Controller.AssignedCell != null)
        {
            Vector3 cellCenter = Controller.AssignedCell.transform.position;
            Vector3 targetPos = targetMono.transform.position;
            float distanceToCell = Vector3.Distance(cellCenter, targetPos);

            if (distanceToCell < 4f) return true;
        }
        else
        {
            // 방 없으면 내 몸 기준
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

    public Transform PlayerTransform { get; private set; }

    public void SetPlayerReference(Transform player)
    {
        this.PlayerTransform = player;
    }
}