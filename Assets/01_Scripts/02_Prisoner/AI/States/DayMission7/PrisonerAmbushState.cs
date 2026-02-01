using UnityEngine;
using UnityEngine.AI;

public class PrisonerAmbushState : BasePrisonerState
{
    private const float AmbushDistance = 4.0f;
    private const float ArrivalDistance = 0.5f; // 도착 허용 오차
    private const float StuckDistance = 1.5f;   // 끼임 방지용 거리 (추가됨)
    private bool _hasArrivedAtSpot = false;

    private const string STATE_ACTION = "Action";
    private const string STATE_RUN = "Run";

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtSpot = false;

        // 이전 상태 정리
        anim.SetBool("IsAction", false);
        anim.SetBool("IsntStanding", false);

        // 이동 시작 세팅
        anim.SetInteger("ActionType", 0);
        anim.SetBool("Run", true);

        // 플레이어 찾기 (안전장치)
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 이동 명령
        if (agent != null && agent.isOnNavMesh)
        {
            if (fsm.InspectionPoint != null)
            {
                // 시작부터 이미 도착 지점 근처라면 바로 대기 상태로 진입
                float dist = Vector3.Distance(fsm.transform.position, fsm.InspectionPoint.position);
                if (dist <= ArrivalDistance)
                {
                    EnterAmbushPose();
                    return;
                }

                agent.isStopped = false;
                agent.SetDestination(fsm.InspectionPoint.position);
                anim.CrossFade(STATE_RUN, 0.1f);
                Debug.Log($"[Ambush] {Controller.name} -> 매복 위치({fsm.InspectionPoint.name})로 이동 시작");
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
        // 1. 목적지 도착 체크 (플레이어 존재 여부와 무관하게 먼저 수행)
        if (!_hasArrivedAtSpot && fsm.InspectionPoint != null)
        {
            CheckArrival();
        }

        // 2. 플레이어 체크 (없으면 기습 로직만 스킵)
        if (player == null) return;

        // 3. 플레이어 기습 감지
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            Controller.StartActionBehavior(0);
            fsm.ChangeState(fsm.CombatState);
        }
    }

    // NavMeshAgent 보정 로직 추가
    private void CheckArrival()
    {
        if (agent.pathPending) return;

        // 1. NavMeshAgent 기준 도착 판정
        bool agentSaysArrived = (agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance);

        // 2. 물리적 거리 기준 강제 판정 (높이 무시)
        Vector3 myPos = fsm.transform.position;
        Vector3 targetPos = fsm.InspectionPoint.position;
        myPos.y = targetPos.y = 0;

        float distance = Vector3.Distance(myPos, targetPos);
        bool distanceSaysArrived = distance <= ArrivalDistance;

        // 3. 끼임 방지: 목표 근처(1.5m)인데 속도가 거의 0이면 도착으로 간주
        bool isStuckNearDestination = (distance <= StuckDistance) && (agent.velocity.sqrMagnitude <= 0.01f);

        // 셋 중 하나라도 만족하면 도착 처리
        if (agentSaysArrived || distanceSaysArrived || isStuckNearDestination)
        {
            // 경로가 없거나, 멈췄거나, 거리상 도착했다면 진입
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.1f || distanceSaysArrived || isStuckNearDestination)
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
            agent.ResetPath();
        }

        // 0. 회전 강제 맞춤
        if (fsm.InspectionPoint != null)
        {
            Controller.transform.rotation = fsm.InspectionPoint.rotation;
        }

        // 애니메이션 파라미터 정리
        anim.SetBool("Run", false);
        anim.SetBool("IsntStanding", true);

        // 행동 시작
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);
        anim.SetInteger("ActionType", 9); // 안전장치

        anim.CrossFade(STATE_ACTION, 0.2f);

        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (Run:False, IsntStanding:True, ActionType:9)");
    }

    public override void Exit()
    {
        anim.SetBool("Run", false);
        anim.SetInteger("ActionType", 0);
        anim.SetBool("IsntStanding", false);
        anim.SetBool("IsAction", false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        Controller.StartActionBehavior(0);
        fsm.ChangeState(fsm.CombatState);
    }
}