using UnityEngine;
using UnityEngine.AI;

public class PrisonerAmbushState : BasePrisonerState
{
    // ★ 기습 인식 범위 (방 중심으로부터의 거리)
    // 방 크기에 맞춰 4.0f ~ 6.0f 정도로 조절하세요.
    private const float AmbushDistance = 6.0f;
    private const float ArrivalDistance = 0.5f;
    private bool _hasArrivedAtSpot = false;

    private const string STATE_ACTION = "Action";
    private const string STATE_RUN = "Run";

    // Animator Hashes (최적화 적용)
    private static readonly int IsActionHash = Animator.StringToHash("IsAction");
    private static readonly int IsntStandingHash = Animator.StringToHash("IsntStanding");
    private static readonly int ActionTypeHash = Animator.StringToHash("ActionType");
    private static readonly int RunHash = Animator.StringToHash("Run");

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtSpot = false;

        anim.SetBool(IsActionHash, false);
        anim.SetBool(IsntStandingHash, false);
        anim.SetInteger(ActionTypeHash, 0);
        anim.SetBool(RunHash, true);

        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            if (fsm.InspectionPoint != null)
            {
                float dist = Vector3.Distance(fsm.transform.position, fsm.InspectionPoint.position);
                if (dist <= ArrivalDistance)
                {
                    EnterAmbushPose();
                    return;
                }

                agent.isStopped = false;
                agent.SetDestination(fsm.InspectionPoint.position);
                anim.CrossFade(STATE_RUN, 0.1f);
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
        // 1. 도착 판정
        if (!_hasArrivedAtSpot && fsm.InspectionPoint != null)
        {
            CheckArrival();
        }

        // 2. 플레이어 재탐색
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
            if (player == null) return;
        }

        // ================================================================
        // ★ [핵심 수정] 기습 판정 기준점 변경 (죄수 -> 방 중심)
        // ================================================================
        Vector3 detectionOrigin;

        // 죄수에게 할당된 방(AssignedCell)이 있다면 방 위치를 기준으로, 없다면 본인 기준
        if (Controller.AssignedCell != null)
            detectionOrigin = Controller.AssignedCell.transform.position;
        else
            detectionOrigin = fsm.transform.position;

        // Y축 차이로 인한 거리 오차 제거
        Vector3 originPos = detectionOrigin;
        Vector3 playerPos = player.position;
        originPos.y = playerPos.y = 0;

        float distToRoom = Vector3.Distance(originPos, playerPos);

        if (distToRoom <= AmbushDistance)
        {
            ExecuteAmbush();
        }
    }

    private void ExecuteAmbush()
    {
        Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 방 근처 플레이어 감지! 기습 개시!</color>");

        // 문 강제 개방 이벤트 발행
        PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

        Controller.StartActionBehavior(0);
        fsm.ChangeState(fsm.CombatState);
    }

    private void CheckArrival()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.1f)
            {
                EnterAmbushPose();
                return;
            }
        }

        Vector3 myPos = fsm.transform.position;
        Vector3 targetPos = fsm.InspectionPoint.position;
        myPos.y = targetPos.y = 0;

        if (Vector3.Distance(myPos, targetPos) <= ArrivalDistance)
        {
            EnterAmbushPose();
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

        if (fsm.InspectionPoint != null)
        {
            Controller.transform.rotation = fsm.InspectionPoint.rotation;
        }

        anim.SetBool(RunHash, false);
        anim.SetBool(IsntStandingHash, true);

        Controller.StartActionBehavior(PrisonerAIType.Ambusher);
        anim.SetInteger(ActionTypeHash, 9);
        anim.CrossFade(STATE_ACTION, 0.2f);
    }

    public override void Exit()
    {
        anim.SetBool(RunHash, false);
        anim.SetInteger(ActionTypeHash, 0);
        anim.SetBool(IsntStandingHash, false);
        anim.SetBool(IsActionHash, false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        ExecuteAmbush();
    }
}