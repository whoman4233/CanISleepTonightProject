using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    private const float AmbushDistance = 4.0f;
    private const float ArrivalDistance = 0.5f;
    private bool _hasArrivedAtSpot = false;

    private const string STATE_ACTION = "Action";
    private const string STATE_RUN = "Run";

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _hasArrivedAtSpot = false;

        // [수정 1] 이동 중에는 "서서" 뛰어야 하므로 false로 설정해야 합니다.
        // (기존 코드는 주석과 반대로 true로 되어 있어, 뛰지 않고 기어가거나 멈칫거렸을 것입니다.)
        anim.SetBool("IsntStanding", false);

        // 이동 시작: ActionType 0, Run 켜기
        anim.SetInteger("ActionType", 0);
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
                anim.CrossFade(STATE_RUN, 0.1f); // Run 상태로 진입
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
        // [핵심 수정] 도착 시 파라미터 강제 설정
        // ================================================================

        // 1. 뛰기 끄기
        anim.SetBool("Run", false);

        // 2. [누락된 부분] 매복 자세를 위해 IsntStanding을 True로 변경
        anim.SetBool("IsntStanding", true);

        // 3. 컨트롤러에 매복 행동 요청 (내부적으로 9번 세팅하겠지만 안전하게 확인)
        Controller.StartActionBehavior(PrisonerAIType.Ambusher);

        // [안전장치] 만약 Controller가 ActionType을 9로 안 바꿔줄 수도 있으니 강제로 세팅
        anim.SetInteger("ActionType", 9);

        // 4. Action 상태로 전환 (ActionType 9 + IsntStanding True -> 매복 애니메이션 재생)
        anim.CrossFade(STATE_ACTION, 0.1f);

        Debug.Log($"[Ambush] 도착 완료. 매복 대기 (Run:False, IsntStanding:True, ActionType:9)");
    }

    public override void Exit()
    {
        anim.SetBool("Run", false);
        anim.SetInteger("ActionType", 0);
        anim.SetBool("IsntStanding", false);

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