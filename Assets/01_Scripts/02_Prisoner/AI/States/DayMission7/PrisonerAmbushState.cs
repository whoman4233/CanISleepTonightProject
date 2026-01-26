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

        // ★ [핵심 1] 이동 시작 시 ActionType을 0으로 초기화하여
        // 9번(매복 자세) 등이 Run 애니메이션을 방해하지 않도록 함.
        anim.SetInteger("ActionType", 0);
        anim.SetBool("IsntStanding", true);

        // ★ [핵심 2] 무조건 Run 파라미터를 true로 설정
        anim.SetBool("Run", true);

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

                // 확실하게 Run 상태로 진입하도록 CrossFade 사용 (안전장치)
                anim.CrossFade("Run", 0.1f);

                Debug.Log($"[Ambush] {Controller.name} -> 매복 위치로 이동 시작 (Run=true, ActionType=0)");
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

        // 1. 기습 감지 (플레이어 접근)
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            // 전투 전환: 무기는 유지하되, 애니메이션만 전투 대기(0)로 변경
            Controller.StartActionBehavior(0);
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

        // ================================================================
        // ★ [핵심 3] 도착 후 처리: Run 끄고 매복 자세(9번)로 즉시 전환
        // ================================================================

        anim.SetBool("Run", false);
        anim.SetInteger("ActionType", 9); // 매복 자세 ID

        // Standing(Idle)을 거치지 않고 바로 ActionIdle로 넘어가도록 강제 블렌딩
        // (애니메이터에 "ActionIdle"이라는 State 이름이 있어야 함)
        anim.CrossFade("ActionIdle", 0.1f);

        // 무기/도구 활성화 (이미 켜져있겠지만 확실하게)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (Run=false, ActionType=9)");
    }

    public override void Exit()
    {
        // 상태 종료 시 정리
        anim.SetBool("Run", false);
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
        // 피격 시 전투 상태로 전환 (무기 유지)
        Controller.StartActionBehavior(0);
        fsm.ChangeState(fsm.CombatState);
    }
}