using UnityEngine;
using System;
using System.Collections;

public class PrisonerQTEApproachState : BasePrisonerState
{
    // ================================================================
    // Animator Hashes 캐싱
    // ================================================================
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int InCombatHash = Animator.StringToHash("InCombat");

    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    private float _originalStoppingDistance;
    private bool _isChasingStarted = false;
    private bool _isQteTriggered = false;

    private Action<QTEEndedEvent> _onQTEEnded;
    private Action<QTEResultAnimationFinishedEvent> _onResultAnimFinished;

    private float _originalSpeed;
    private const float QTE_APPROACH_SPEED = 12.0f;
    private bool _ended;
    private Coroutine _safetyCoroutine;

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();
    }

    public override void Enter()
    {
        _onResultAnimFinished = OnResultAnimationFinished;
        _onQTEEnded = OnQTEEnded;

        EventBus.Subscribe(_onResultAnimFinished);
        EventBus.Subscribe(_onQTEEnded);

        _isChasingStarted = false;
        _isQteTriggered = false;
        _safetyCoroutine = null;

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
            // [수정] Hash 사용
            anim.SetBool(WalkHash, false);
        }
    }

    public override void Update()
    {
        if (_ended) return;

        if (_isQteTriggered) return;

        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
            if (player == null) return;
        }

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);
            if (agent.pathPending) return;
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                _isQteTriggered = true;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;

                // [수정] Hash 사용
                anim.SetBool(WalkHash, false);
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
        if (_isChasingStarted) return;
        _isChasingStarted = true;
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            _originalStoppingDistance = agent.stoppingDistance;
            agent.stoppingDistance = fsm.QteStopDistance;
        }

        // [수정] Hash 사용
        anim.SetBool(WalkHash, true);
    }

    private void OnQTEEnded(QTEEndedEvent evt)
    {
        if (evt.Action != qteAction) return;
        if (PrisonerQTEContext.CurrentAttacker != fsm.transform.gameObject) return;

        // [수정] Hash 사용
        anim.SetBool(InCombatHash, true);

        if (_safetyCoroutine != null) fsm.StopCoroutine(_safetyCoroutine);
        _safetyCoroutine = fsm.StartCoroutine(CoSafetyFallback());
    }

    private IEnumerator CoSafetyFallback()
    {
        yield return new WaitForSeconds(2.0f);
        Debug.LogWarning($"[PrisonerFSM] {fsm.name} QTE 애니메이션 이벤트 누락 감지! 강제로 전투 상태로 전환합니다.");
        TransitionToCombat();
    }

    private void OnResultAnimationFinished(QTEResultAnimationFinishedEvent evt)
    {
        if (evt.Action != qteAction) return;
        if (PrisonerQTEContext.CurrentAttacker != fsm.transform.gameObject) return;

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