using UnityEngine;
using UnityEngine.AI; // NavMeshAgent 사용을 위해 명시

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

        // [수정 1] 이동 중에는 "서서" 뛰어야 하므로 false로 설정
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
                anim.CrossFade(STATE_RUN, 0.1f); // Run 상태로 진입
                Debug.Log($"[Ambush] {Controller.name} -> 매복 위치({fsm.InspectionPoint.name})로 이동 시작");
            }
            else
            {
                // 이동할 포인트가 없으면 즉시 매복 자세
                EnterAmbushPose();
            }
        }
        else
        {
            // NavMesh 위에 없으면 즉시 매복 자세
            EnterAmbushPose();
        }
    }

    public override void Update()
    {
        if (player == null) return;

        // 1. 플레이어 기습 감지 (도착 여부와 상관없이 사거리 들어오면 공격)
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            Controller.StartActionBehavior(0);
            fsm.ChangeState(fsm.CombatState);
            return;
        }

        // 2. 목적지 도착 체크 (보강된 로직)
        if (!_hasArrivedAtSpot && fsm.InspectionPoint != null)
        {
            CheckArrival();
        }
    }

    // ★ [보강] 도착 체크 로직 분리 및 강화
    private void CheckArrival()
    {
        // 1. 경로 계산 중이면 판단 보류 (PathPending이 true면 remainingDistance가 0일 수 있음)
        if (agent.pathPending) return;

        // 2. 경로가 유효하지 않거나 끊겼을 때의 안전장치
        // remainingDistance가 Infinity면 도달 불가능한 상태
        if (float.IsPositiveInfinity(agent.remainingDistance)) return;

        // 3. 실제 도착 판정
        // (remainingDistance가 유효하고, 설정한 거리보다 가까워졌을 때)
        if (agent.remainingDistance <= agent.stoppingDistance + ArrivalDistance)
        {
            // [추가 검증] 실제로 경로가 있는지 확인 (NavMesh 버그로 인한 0 방지)
            if (agent.hasPath || agent.velocity.sqrMagnitude <= 0.1f)
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
            agent.ResetPath(); // 경로 초기화로 미세 떨림 방지
        }

        // ================================================================
        // [핵심 수정] 도착 시 파라미터 및 회전 강제 설정
        // ================================================================

        // 0. [보강] 도착했으면 매복 지점이 바라보는 방향으로 회전 맞춤 (벽 등지고 숨기 등)
        if (fsm.InspectionPoint != null)
        {
            Controller.transform.rotation = fsm.InspectionPoint.rotation;
        }

        // 1. 뛰기 끄기
        anim.SetBool("Run", false);

        // 2. 매복 자세를 위해 IsntStanding을 True로 변경
        anim.SetBool("IsntStanding", true);

        // 3. 컨트롤러에 매복 행동 요청 (내부적으로 9번 세팅하겠지만 안전하게 확인)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        // [안전장치] 만약 Controller가 ActionType을 9로 안 바꿔줄 수도 있으니 강제로 세팅
        anim.SetInteger("ActionType", 9);

        // 4. Action 상태로 전환 (ActionType 9 + IsntStanding True -> 매복 애니메이션 재생)
        anim.CrossFade(STATE_ACTION, 0.2f); // 0.1f -> 0.2f로 조금 더 부드럽게

        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (Run:False, IsntStanding:True, ActionType:9)");
    }

    public override void Exit()
    {
        anim.SetBool("Run", false);
        anim.SetInteger("ActionType", 0);
        anim.SetBool("IsntStanding", false);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false; // [수정] 나갈 때는 다시 움직일 수 있게 풀어줌
            agent.ResetPath();
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 시 전투 상태로 전환
        Controller.StartActionBehavior(0);
        fsm.ChangeState(fsm.CombatState);
    }
}