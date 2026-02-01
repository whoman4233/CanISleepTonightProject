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

        // ★ [중요] 이전 상태(Combat 등)에서 켜졌을 수 있는 IsAction을 확실히 끕니다.
        // 이게 켜져 있으면 Run -> Action으로 멋대로 튀거나 블렌딩이 꼬일 수 있습니다.
        anim.SetBool("IsAction", false);

        // 이동 중에는 "서서" 뛰어야 하므로 false로 설정
        anim.SetBool("IsntStanding", false);

        // 이동 시작: ActionType 0, Run 켜기
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
        if (player == null) return;

        // 1. 플레이어 기습 감지
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            Controller.StartActionBehavior(0);
            fsm.ChangeState(fsm.CombatState);
            return;
        }

        // 2. 목적지 도착 체크
        if (!_hasArrivedAtSpot && fsm.InspectionPoint != null)
        {
            CheckArrival();
        }
    }

    // ★ [보강] NavMeshAgent가 멍청하게 굴 때를 대비한 2중 체크
    private void CheckArrival()
    {
        if (agent.pathPending) return;

        // 1. NavMeshAgent 기준 도착 판정
        bool agentSaysArrived = (agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance);

        // 2. [추가] 물리적 거리 기준 강제 판정 (Agent가 벽에 걸려서 remainingDistance가 안 줄어들 때 대비)
        // Y축 높이 차이는 무시하고 수평 거리만 계산 (2D 거리 체크가 더 정확함)
        Vector3 myPos = fsm.transform.position;
        Vector3 targetPos = fsm.InspectionPoint.position;
        myPos.y = targetPos.y = 0; // 높이 무시

        bool distanceSaysArrived = Vector3.Distance(myPos, targetPos) <= ArrivalDistance;

        // 둘 중 하나라도 만족하면 도착으로 간주
        if (agentSaysArrived || distanceSaysArrived)
        {
            // 경로가 있거나, 혹은 이미 멈춰있다면 진입
            if (!agent.hasPath || agent.velocity.sqrMagnitude <= 0.1f || distanceSaysArrived)
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

        // ★ [핵심] 애니메이션 파라미터 정리 순서
        // 1. Run 끄기
        anim.SetBool("Run", false);

        // 2. 자세 잡기
        anim.SetBool("IsntStanding", true);

        // 3. 행동 시작 (IsAction = true가 여기서 세팅됨)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        // 4. 안전장치: ActionType 9번 강제
        anim.SetInteger("ActionType", 9);

        // 5. 전환
        anim.CrossFade(STATE_ACTION, 0.2f);

        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (Run:False, IsntStanding:True, ActionType:9)");
    }

    public override void Exit()
    {
        anim.SetBool("Run", false);
        anim.SetInteger("ActionType", 0);
        anim.SetBool("IsntStanding", false);

        // ★ [추가] 나갈 때 IsAction도 꺼줘야 다음 상태(Return 등)에서 안 꼬임
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