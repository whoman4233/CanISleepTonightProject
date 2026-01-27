using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    private const float AmbushDistance = 4.0f;
    private const float ArrivalDistance = 0.5f;
    private bool _hasArrivedAtSpot = false;

    // 애니메이터 State 이름 상수 (컨트롤러 내 실제 State 이름과 일치해야 함)
    private const string STATE_ACTION = "Action";
    private const string STATE_RUN = "Run";

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtSpot = false;

        // [수정 1] 이동 중에는 서있는 상태여야 함 -> IsntStanding = false (안전장치)
        // (true일 경우 Run 대신 Sit/Sleep 등으로 빠질 위험이 있음)
        anim.SetBool("IsntStanding", true);

        // [핵심 1] 이동 시작 시 ActionType 0 (일반 이동)
        anim.SetInteger("ActionType", 0);

        // [핵심 2] Run 켜기
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

                // [수정 2] "Run" 상태로 안전하게 전환
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
        if (player == null) return;

        // 1. 기습 감지
        float distToPlayer = Vector3.Distance(fsm.transform.position, player.position);
        if (distToPlayer <= AmbushDistance)
        {
            Debug.Log($"<color=red>[Ambush] {Controller.Data.ID} : 기습 개시!</color>");
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            // 전투 전환 (Controller에서 다른 무기는 끄고 내 무기는 켬)
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
        // [핵심 3] 도착 후 처리: Run 끄고 매복 자세(9번) 전환
        // ================================================================

        anim.SetBool("Run", false);

        // 1. Controller를 통해 무기 켜기 & 파라미터 세팅
        // (Controller 수정본을 적용했다면 여기서 다른 무기는 꺼지고 단검만 켜짐)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        // 2. [수정 3] "Action" 상태로 전환 ("ActionIdle"이라는 State가 없을 확률 높음)
        // ActionType이 9로 설정되었으므로 Action State 내부에서 매복 모션이 재생됨
        anim.CrossFade(STATE_ACTION, 0.1f);

        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (ActionType: 9)");
    }

    public override void Exit()
    {
        // 상태 종료 시 정리
        anim.SetBool("Run", false);
        anim.SetInteger("ActionType", 0);
        anim.SetBool("IsntStanding", false); // 나갈 때도 리셋

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
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