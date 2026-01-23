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

    private IPrisonerState _currentState;

    // ... (이벤트 핸들러 및 상태 변수들은 기존과 동일) ...

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

    public bool IsInvulnerable => _currentState == InspectionState || _currentState == DeadState;

    private void Awake()
    {
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

        if (defaultQteAction == null)
            Debug.LogWarning($"[PrisonerFSM] {name} : QTE Action Data is missing in Inspector!");

        QTEApproachState = new PrisonerQTEApproachState(this, defaultQteAction);

        // 이벤트 변수 초기화
        _onInspectionStarted = OnInspectionStarted;
        _onInspectionEnded = OnInspectionEnded;
    }

    // ... (OnEnable, OnDisable, Setup, InitializeBehavior 등 기존 로직 유지) ...

    private Action<InspectionStartedEvent> _onInspectionStarted;
    private Action<InspectionEndedEvent> _onInspectionEnded;

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
            this.InspectionPoint = controller.AssignedCell.inspectionPoint;

        ActionState.SetActionType(PrisonerAIType.Good);
        ChangeState(ActionState);
    }

    public void InitializeBehavior(PrisonerAIType aiType)
    {
        float runStyleValue = (aiType == PrisonerAIType.Escaper) ? 1f : 0f;
        Anim.SetFloat("RunStyle", runStyleValue);

        if (CheckAndEnterVisualState()) return;

        if (aiType == PrisonerAIType.Ambusher)
        {
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
                ChangeState(InspectionState);
                break;
            case PrisonerAIType.Escaper:
                Debug.Log($"[FSM] {name} 탈주 시작!");
                break;
        }
    }

    public void BackToRoutine()
    {
        if (_currentState == DeadState) return;
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
    // 기습 공격 로직
    // ================================================================

    private void OnInspectionStarted(InspectionStartedEvent evt)
    {
        if (_currentState == DeadState || _currentState == CowerState || _currentState == CombatState)
            return;

        if (!IsTargetRelatedToMe(evt.Target))
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

    private bool CheckAndEnterVisualState()
    {
        VisualAnomalyType myVisual = GetMyVisualType();
        if (myVisual == VisualAnomalyType.BikiniModel) { ChangeState(BikiniState); return true; }
        if (IsVisualIdleTarget(myVisual)) { ChangeState(VisualIdleState); return true; }
        return false;
    }
    private VisualAnomalyType GetMyVisualType()
    {
        if (PrisonerScheduleManager.Instance != null && Controller != null && Controller.AssignedCell != null)
            return PrisonerScheduleManager.Instance.GetDailyRole(Controller.AssignedCell.cellId).visualType;
        return VisualAnomalyType.None;
    }
    private bool IsVisualIdleTarget(VisualAnomalyType type) => type != VisualAnomalyType.None && type != VisualAnomalyType.BikiniModel;
    private bool IsIgnoreInspectionType(PrisonerAIType type) => type == PrisonerAIType.Ambusher || type == PrisonerAIType.Singing || type == PrisonerAIType.Screaming || type == PrisonerAIType.Crying || type == PrisonerAIType.Mumbling || type == PrisonerAIType.HammeringWall || type == PrisonerAIType.Deadlift;
    private bool IsCenterSpawnType() => GetMyVisualType().ToString().StartsWith("PSN_Franke") || GetMyVisualType().ToString().StartsWith("Suspect");
}