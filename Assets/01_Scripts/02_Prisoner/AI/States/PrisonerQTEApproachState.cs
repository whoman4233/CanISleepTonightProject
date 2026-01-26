using UnityEngine;

public class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    private float _originalStoppingDistance;
    private bool _isChasingStarted = false; // 추격 세팅 완료 여부

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();
    }

    public override void Enter()
    {
        _isChasingStarted = false;

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
        // ★ 플레이어가 없어도 에러 내고 Idle로 돌아가지 않음.
        //    Update에서 플레이어가 생길 때까지 대기함.
    }

    public override void Update()
    {
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
                // 아직도 플레이어가 없으면 이번 프레임은 대기
                return;
            }
        }

        // 2. 추격 로직 (플레이어가 존재함이 보장됨)
        agent.SetDestination(player.position);

        if (agent.pathPending) return;

        // 설정한 거리(QteStopDistance) 이내에 도달하면 QTE 시작
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
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

            // 2. InspectionState로 전환
            fsm.ChangeState(fsm.InspectionState);
        }
    }

    public override void Exit()
    {
        agent.ResetPath();

        // 추격 세팅이 되었을 때만 복구 수행
        if (_isChasingStarted)
        {
            agent.stoppingDistance = _originalStoppingDistance;
        }

        anim.SetBool("Walk", false);
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
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