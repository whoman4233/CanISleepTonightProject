using UnityEngine;

public class PrisonerQTEApproachState : BasePrisonerState
{
    private QTEActionSO qteAction;
    private QTEDistanceTrigger _trigger;

    // ★ 기존 정지 거리를 저장했다가 복구하기 위한 변수
    private float _originalStoppingDistance;

    public PrisonerQTEApproachState(PrisonerFSM fsm, QTEActionSO action) : base(fsm)
    {
        this.qteAction = action;
        _trigger = fsm.GetComponent<QTEDistanceTrigger>();
    }

    public override void Enter()
    {
        agent.isStopped = false;
        anim.SetBool("Walk", true);

        // ★ [수정] FSM에 설정된 QTE 정지 거리 적용
        _originalStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = fsm.QteStopDistance;

        agent.SetDestination(player.position);
    }

    public override void Update()
    {
        agent.SetDestination(player.position);

        if (agent.pathPending) return;

        //remainingDistance가 stoppingDistance(설정한 거리) 이내가 되면 공격 시작
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            PrisonerQTEContext.SetAttacker(fsm.transform);

            if (_trigger != null)
            {
                _trigger.NotifyArrived();
            }
            else
            {
                if (qteAction != null)
                    EventBus.Publish(new QTEStartedEvent { Action = qteAction });
            }

            // 도착했으므로 Idle 상태로 전환 (이후 QTE 애니메이션은 다른 스크립트가 처리)
            fsm.ChangeState(fsm.CombatState);
        }
    }

    public override void Exit()
    {
        agent.ResetPath();

        // ★ [수정] 정지 거리 원상 복구 (다른 상태에서 문제 없도록)
        agent.stoppingDistance = _originalStoppingDistance;

        anim.SetBool("Walk", false);
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
    }
}