using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    private const float AmbushDistance = 4.0f;
    private const float ArrivalDistance = 0.5f;
    private bool _hasArrivedAtSpot = false;

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtSpot = false;

        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            if (fsm.InspectionPoint != null)
            {
                agent.isStopped = false;
                agent.SetDestination(fsm.InspectionPoint.position);

                anim.SetBool("Run", true);

                // [기존] 애니메이션 강제 전환
                anim.CrossFade("Run", 0.1f);

                Debug.Log($"[Ambush] {Controller.name} -> 매복 위치로 이동 중...");
            }
            else
            {
                EnterAmbushPose();
            }
        }
        else
        {
            EnterAmbushPose();
        }
    }

    public override void Update()
    {
        if (player == null) return;

        // 1. 플레이어 감지 (기습)
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            Controller.StopActionBehavior();
            fsm.ChangeState(fsm.CombatState);
            return;
        }

        // 2. 목적지 도착 체크
        if (!_hasArrivedAtSpot && fsm.InspectionPoint != null)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance)
            {
                EnterAmbushPose();
            }
        }
    }

    private void EnterAmbushPose()
    {
        if (_hasArrivedAtSpot) return;
        _hasArrivedAtSpot = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        anim.SetBool("Run", false);

        // ================================================================
        // ★ [추가됨] 도착 후 ActionIdle로 전환 (ActionType 9번)
        // ================================================================
        // Animator에서 Run -> ActionIdle (Condition: ActionType == 9)로 설정되어 있어야 함
        anim.SetInteger("ActionType", 9);

        // 매복 자세 취하기
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);
        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (ActionType: 9).");
    }

    public override void Exit()
    {
        Controller.StopActionBehavior();
        anim.SetBool("Run", false);

        // ================================================================
        // ★ [추가됨] 상태 종료 시 ActionType 초기화
        // ================================================================
        // 초기화하지 않으면 전투 상태로 넘어가서도 9번 모션을 취할 위험이 있음
        anim.SetInteger("ActionType", 0);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        Controller.StopActionBehavior();
        fsm.ChangeState(fsm.CombatState);
    }
}