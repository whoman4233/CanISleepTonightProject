using UnityEngine;
using System; // Action 사용을 위해 추가

public class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    private float _originalStoppingDistance;
    private bool _isChasingStarted = false; // 추격 세팅 완료 여부

    // ★ [추가] QTE가 실행되었는지 체크하는 플래그
    private bool _isQteTriggered = false;

    // ★ [추가] 이벤트 핸들러 캐싱
    private Action<QTEEndedEvent> _onQteEndedHandler;

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();

        // 핸들러 연결
        _onQteEndedHandler = OnQteEnded;
    }

    public override void Enter()
    {
        _isChasingStarted = false;
        _isQteTriggered = false;

        // ★ [핵심] QTE 종료 이벤트 구독
        EventBus.Subscribe(_onQteEndedHandler);

        // 1. 플레이어 찾기 시도
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 2. 플레이어가 있다면 즉시 추격 시작
        if (player != null)
        {
            StartChasing();
        }
    }

    public override void Update()
    {
        // ★ [추가] 이미 QTE를 걸었다면 종료 이벤트가 올 때까지 대기 (아무것도 안 함)
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
        agent.SetDestination(player.position);

        if (agent.pathPending) return;

        // 설정한 거리(QteStopDistance) 이내에 도달하면 QTE 시작
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // ★ [수정] 도착 처리 및 QTE 실행
            _isQteTriggered = true;

            // 이동 정지 및 애니메이션 끄기
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Walk", false);

            PrisonerQTEContext.SetAttacker(fsm.transform);

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

            // ★ [핵심] 여기서 ChangeState를 호출하지 않습니다!
            // QTE가 끝날 때까지(OnQteEnded 호출 시까지) 현재 상태를 유지하며 대기합니다.
            // fsm.ChangeState(fsm.InspectionState); // <-- 삭제됨
        }
    }

    // ★ [추가] QTE 종료 시 호출되는 콜백
    private void OnQteEnded(QTEEndedEvent evt)
    {
        // 내가 실행한 QTE인지 확인
        if (evt.Action == this.qteAction)
        {
            // 전투 준비: 애니메이션 파라미터 초기화 (필요 시)
            fsm.Controller.StartActionBehavior(0);

            // 전투 상태로 전환 (QTE 끝났으니 싸우자)
            fsm.ChangeState(fsm.CombatState);
        }
    }

    public override void Exit()
    {
        // ★ [핵심] 나갈 때 반드시 구독 해제
        EventBus.Unsubscribe(_onQteEndedHandler);

        agent.ResetPath();

        // 추격 세팅이 되었을 때만 복구 수행
        if (_isChasingStarted)
        {
            agent.stoppingDistance = _originalStoppingDistance;
        }

        // 상태 종료 시 확실하게 멈춤 처리
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        anim.SetBool("Walk", false);
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

        agent.isStopped = false;
        anim.SetBool("Walk", true);

        // FSM에 설정된 QTE 정지 거리 적용
        _originalStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = fsm.QteStopDistance;

        Debug.Log($"[PrisonerQTE] {fsm.name} : 플레이어 발견! 추격 시작.");
    }
}