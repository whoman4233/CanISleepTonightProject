using UnityEngine;

public class PrisonerAmbushState : BasePrisonerState
{
    // 기습 감지 거리 (문 뒤에서 플레이어 감지)
    private const float AmbushDistance = 3.5f;

    public PrisonerAmbushState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        // 1. 이동 멈춤 (문 뒤에 숨기)
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // 2. 숨어있는 애니메이션 (없으면 Idle)
        anim.SetBool("Walk", false);
        anim.SetBool("Run", false);
        // anim.SetTrigger("Hide"); // 숨는 모션이 있다면 사용
    }

    public override void Update()
    {
        // 플레이어가 없는 경우 리턴
        if (player == null) return;

        // 1. 거리 계산
        float dist = Vector3.Distance(fsm.transform.position, player.position);

        // 2. 사거리 안에 들어오면 기습 시작
        if (dist <= AmbushDistance)
        {
            Debug.Log($"[Ambush] {Controller.Data.ID} : 놈이 왔다! 기습 개시!");

            // 문을 강제로 엶 (이펙트/소리 추가 가능)
            PrisonerEventBus.PublishForceOpenDoor(Controller.Data.CellID);

            // 전투 상태로 전환 -> 추격 및 공격 시작
            fsm.ChangeState(fsm.CombatState);
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 숨어있는데 맞았다? 바로 전투 돌입
        fsm.ChangeState(fsm.CombatState);
    }
}