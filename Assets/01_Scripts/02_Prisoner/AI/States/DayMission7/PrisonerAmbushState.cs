using UnityEngine;
using UnityEngine.AI;

public class PrisonerAmbushState : BasePrisonerState
{
    private const float AmbushDistance = 4.0f;
    private const float ArrivalDistance = 0.5f; // 도착 허용 오차
    private bool _hasArrivedAtSpot = false;

    private const string STATE_ACTION = "Action";
    private const string STATE_RUN = "Run";

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtSpot = false;

        anim.SetBool("IsAction", false);
        anim.SetBool("IsntStanding", false);
        anim.SetInteger("ActionType", 0);
        anim.SetBool("Run", true);

        // Enter 시점에 플레이어 찾기 시도
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
                // 이미 아주 가까우면 바로 대기
                float dist = Vector3.Distance(fsm.transform.position, fsm.InspectionPoint.position);
                if (dist <= ArrivalDistance)
                {
                    EnterAmbushPose();
                    return;
                }

                agent.isStopped = false;
                agent.SetDestination(fsm.InspectionPoint.position);
                anim.CrossFade(STATE_RUN, 0.1f);
                Debug.Log($"[Ambush] {Controller.name} -> 매복 위치로 이동 시작");
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
        // 1. 도착 판정 (플레이어 유무와 상관없이 항상 체크)
        if (!_hasArrivedAtSpot && fsm.InspectionPoint != null)
        {
            CheckArrival();
        }

        // 2. 플레이어 재탐색 로직 (★ 중요: 없으면 찾을 때까지 시도)
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null)
            {
                player = pObj.transform;
            }
            else
            {
                // 이번 프레임에도 없으면 기습 로직 실행 불가 -> 리턴
                return;
            }
        }

        // 3. 기습 공격 판정 (플레이어가 존재할 때만)
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            Controller.StartActionBehavior(0);
            fsm.ChangeState(fsm.CombatState);
        }
    }

    private void CheckArrival()
    {
        if (agent.pathPending) return;

        // 1. NavMeshAgent가 도착했다고 하는가?
        if (agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.1f)
            {
                EnterAmbushPose();
                return;
            }
        }

        // 2. 물리적 거리가 가까운가? (Y축 무시)
        Vector3 myPos = fsm.transform.position;
        Vector3 targetPos = fsm.InspectionPoint.position;
        myPos.y = targetPos.y = 0; // 높이 오차 무시

        float distance = Vector3.Distance(myPos, targetPos);
        if (distance <= ArrivalDistance)
        {
            EnterAmbushPose();
        }

        // ★ 3. 이상한 곳에서 멈추는 원인이었던 'StuckDistance(끼임 보정)' 로직 삭제함
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

        // 0. 도착 후 회전 맞춤 (목적지에 도착했을 때만 돌려야 함)
        if (fsm.InspectionPoint != null)
        {
            Controller.transform.rotation = fsm.InspectionPoint.rotation;
        }

        anim.SetBool("Run", false);
        anim.SetBool("IsntStanding", true);

        Controller.StartActionBehavior(PrisonerAIType.Ambusher);
        anim.SetInteger("ActionType", 9);

        anim.CrossFade(STATE_ACTION, 0.2f);
        Debug.Log($"[Ambush] 도착 완료. 매복 대기");
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