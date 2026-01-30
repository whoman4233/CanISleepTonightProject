using UnityEngine;
using System;
using System.Collections;

public class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    private float _originalStoppingDistance;
    private bool _isChasingStarted = false;
    private bool _isQteTriggered = false;

    // ★ [추가] QTE 종료 이벤트도 감지해야 함 (타이머 시작용)
    private Action<QTEEndedEvent> _onQTEEnded;
    private Action<QTEResultAnimationFinishedEvent> _onResultAnimFinished;

    private float _originalSpeed;
    private const float QTE_APPROACH_SPEED = 12.0f;
    private bool _ended;
    // ★ [추가] 안전장치 코루틴
    private Coroutine _safetyCoroutine;

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();
    }

    public override void Enter()
    {
        _onResultAnimFinished = OnResultAnimationFinished;
        _onQTEEnded = OnQTEEnded; // 핸들러 연결

        EventBus.Subscribe(_onResultAnimFinished);
        EventBus.Subscribe(_onQTEEnded); // 구독

        _isChasingStarted = false;
        _isQteTriggered = false;
        _safetyCoroutine = null; // 초기화

        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (agent != null)
        {
            _originalSpeed = agent.speed;
            agent.speed = QTE_APPROACH_SPEED;
        }

        if (player != null)
        {
            StartChasing();
        }
    }

    public override void Exit()
    {
        _isQteTriggered = false;

        // ★ 구독 해제 및 코루틴 정리
        EventBus.Unsubscribe(_onResultAnimFinished);
        EventBus.Unsubscribe(_onQTEEnded);

        if (_safetyCoroutine != null)
        {
            fsm.StopCoroutine(_safetyCoroutine);
            _safetyCoroutine = null;
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.speed = _originalSpeed;

            if (_isChasingStarted)
            {
                agent.stoppingDistance = _originalStoppingDistance;
            }
        }

        if (anim != null)
        {
            anim.SetBool("Walk", false);
        }
    }

    // ... (Update, OnDamaged, StartChasing, Co_StartQTE_NextFrame 등은 기존 코드 유지) ...
    public override void Update()
    {
        // (기존 코드와 동일하므로 생략)
        // ...
        if (_ended) return;

        if (_isQteTriggered) return;

        if (player == null) { /* 플레이어 찾기 로직... */ }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
            if (agent.pathPending) return;
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // (기존 코드)
                _isQteTriggered = true;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                anim.SetBool("Walk", false);
                fsm.StartCoroutine(Co_StartQTE_NextFrame());
            }
        }
    }

    private IEnumerator Co_StartQTE_NextFrame()
    {
        yield return null;
        if (_trigger != null) _trigger.NotifyArrived();
        else if (qteAction != null) EventBus.Publish(new QTEStartedEvent { Action = qteAction });
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir) { }

    private void StartChasing()
    {
        // (기존 코드 유지)
        if (_isChasingStarted) return;
        _isChasingStarted = true;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            _originalStoppingDistance = agent.stoppingDistance;
            agent.stoppingDistance = fsm.QteStopDistance;
        }
        anim.SetBool("Walk", true);
    }

    // ========================================================================
    // ★ [핵심 추가] QTE 결과가 나오면 안전장치 타이머를 돌린다.
    // ========================================================================
    private void OnQTEEnded(QTEEndedEvent evt)
    {
        if (evt.Action != qteAction) return;
        if (PrisonerQTEContext.CurrentAttacker != fsm.transform.gameObject) return;

        // QTE 결과가 나왔으니, 애니메이션이 끝나기를 기다림.
        // 하지만 혹시 이벤트가 안 올 것을 대비해 4초 뒤 강제 전환 예약

        anim.SetBool("InCombat", true);

        if (_safetyCoroutine != null) fsm.StopCoroutine(_safetyCoroutine);
        _safetyCoroutine = fsm.StartCoroutine(CoSafetyFallback());
    }

    private IEnumerator CoSafetyFallback()
    {
        // 애니메이션 길이보다 넉넉하게 대기 (예: 4초)
        yield return new WaitForSeconds(2.0f);

        Debug.LogWarning($"[PrisonerFSM] {fsm.name} QTE 애니메이션 이벤트 누락 감지! 강제로 전투 상태로 전환합니다.");

        // 강제 전환
        TransitionToCombat();
    }

    private void OnResultAnimationFinished(QTEResultAnimationFinishedEvent evt)
    {
        if (evt.Action != qteAction) return;
        if (PrisonerQTEContext.CurrentAttacker != fsm.transform.gameObject) return;

        // 정상적으로 이벤트가 왔으므로 안전장치 해제
        if (_safetyCoroutine != null)
        {
            fsm.StopCoroutine(_safetyCoroutine);
            _safetyCoroutine = null;
        }

        TransitionToCombat();
    }

    private void TransitionToCombat()
    {
        if (_ended) return;
        _ended = true;

        PrisonerQTEContext.Clear();

        fsm.ChangeState(fsm.CombatState);
    }
}