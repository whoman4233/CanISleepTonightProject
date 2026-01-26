using UnityEngine;

public class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    private float _originalStoppingDistance;

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();
    }

    public override void Enter()
    {
        // 플레이어 참조가 끊겼거나 없을 경우 다시 찾기 (NRE 방지)
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;

            // 그래도 없으면 에러 로그 출력 후 복귀
            if (player == null)
            {
                Debug.LogError($"[PrisonerQTE] {fsm.name} : Player를 찾을 수 없습니다! Idle로 복귀합니다.");
                fsm.ChangeState(fsm.ActionState);
                return;
            }
        }

        agent.isStopped = false;
        anim.SetBool("Walk", true);

        // FSM에 설정된 QTE 정지 거리 적용
        _originalStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = fsm.QteStopDistance;

        // 플레이어 위치로 이동
        agent.SetDestination(player.position);
    }

    public override void Update()
    {
        // ★ [추가] Update에서도 플레이어 체크 (안전성 강화)
        if (player == null) return;

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

            // 2. InspectionState로 전환 (QTE 중 이동 방지)
            fsm.ChangeState(fsm.InspectionState);
        }
    }

    public override void Exit()
    {
        agent.ResetPath();

        // 정지 거리 원상 복구
        agent.stoppingDistance = _originalStoppingDistance;

        anim.SetBool("Walk", false);
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
    }
}