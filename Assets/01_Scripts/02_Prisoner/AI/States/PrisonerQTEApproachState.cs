using UnityEngine;
using System;
using System.Collections;

public class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    private float _originalStoppingDistance;
    private bool _isChasingStarted = false; // 추격 세팅 완료 여부

    // QTE가 실행되었는지 체크하는 플래그
    private bool _isQteTriggered = false;

    // 이벤트 핸들러 캐싱
    private Action<QTEResultAnimationFinishedEvent> _onResultAnimFinished; // FSM QTE 종료 애니메이션 전환용

    // ★ [추가] 속도 제어 변수
    private float _originalSpeed;
    private const float QTE_APPROACH_SPEED = 6.0f; // QTE 접근 시 적용할 빠른 속도

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();
    }

    public override void Enter()
    {
        // FSM 전환용 QTE 종료 애니메이션 구독
        _onResultAnimFinished = OnResultAnimationFinished;
        EventBus.Subscribe(_onResultAnimFinished);

        _isChasingStarted = false;
        _isQteTriggered = false;

        // 1. 플레이어 찾기 시도
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // ★ [추가] 속도 높이기 (원래 속도 백업 -> 가속)
        if (agent != null)
        {
            _originalSpeed = agent.speed; // 원래 속도 저장 (보통 3.5)
            agent.speed = QTE_APPROACH_SPEED; // 6.0으로 가속
        }

        // 2. 플레이어가 있다면 즉시 추격 시작
        if (player != null)
        {
            StartChasing();
        }
    }

    public override void Update()
    {
        // 이미 QTE를 걸었다면 종료 이벤트가 올 때까지 대기 (아무것도 안 함)
        if (_isQteTriggered) return;

        // 1. 플레이어가 아직 없다면 계속 찾기 (생성 대기)
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform;
                StartChasing(); // 찾았으니 추격 세팅 적용
            }
            else
            {
                return;
            }
        }

        // 2. 추격 로직 (플레이어가 존재함이 보장됨)
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.position);

            if (agent.pathPending) return;

            // 설정한 거리(QteStopDistance) 이내에 도달하면 QTE 시작
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                // 도착 처리 및 QTE 실행
                _isQteTriggered = true;

                // 이동 정지 및 애니메이션 끄기
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                anim.SetBool("Walk", false);

                PrisonerQTEContext.SetAttacker(fsm.transform);

                fsm.StartCoroutine(Co_StartQTE_NextFrame());
            }
        }
    }

    private IEnumerator Co_StartQTE_NextFrame()
    {
        yield return null; // 한 프레임 딜레이

        // 1. QTE 트리거 발동
        if (_trigger != null)
        {
            _trigger.NotifyArrived();
        }
        else
        {
            if (qteAction != null)
                EventBus.Publish(new QTEStartedEvent { Action = qteAction });
        }
    }

    public override void Exit()
    {
        // 나갈 때 반드시 구독 해제
        _isQteTriggered = false;
        EventBus.Unsubscribe(_onResultAnimFinished);

        // Agent 안전 체크 및 복구
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;

            // ★ [추가] 속도 원상복구 (이걸 안 하면 CombatState에서도 계속 빠름)
            agent.speed = _originalSpeed;

            // 추격 세팅이 되었을 때만 정지 거리 복구 수행
            if (_isChasingStarted)
            {
                agent.stoppingDistance = _originalStoppingDistance;
            }
        }

        // 애니메이션 정리
        if (anim != null)
        {
            anim.SetBool("Walk", false);
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // QTE 도중에는 피격되어도 상태가 바뀌지 않도록 비워둠 (연출 보호)
    }

    // 내부 헬퍼: 추격 시작 시 한 번만 실행되는 설정
    private void StartChasing()
    {
        if (_isChasingStarted) return;
        _isChasingStarted = true;

        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.isStopped = false;
            // FSM에 설정된 QTE 정지 거리 적용
            _originalStoppingDistance = agent.stoppingDistance;
            agent.stoppingDistance = fsm.QteStopDistance;
        }

        anim.SetBool("Walk", true);

        Debug.Log($"[PrisonerQTE] {fsm.name} : 플레이어 발견! 추격 시작 (Speed: {QTE_APPROACH_SPEED})");
    }

    private void OnResultAnimationFinished(QTEResultAnimationFinishedEvent evt)
    {
        // 1. 액션 타입 일치 확인
        if (evt.Action != qteAction) return;

        // ★ [추가] 안전장치: 이 QTE를 실행한 사람이 '나'인지 확인
        // (만약 A, B가 동시에 QTE를 시도했다면, 내가 건 QTE가 끝났을 때만 반응해야 함)
        if (PrisonerQTEContext.CurrentAttacker != fsm.transform) return;

        _isQteTriggered = false;
        fsm.ChangeState(fsm.CombatState);
    }
}