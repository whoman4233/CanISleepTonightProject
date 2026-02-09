using UnityEngine;

public class PrisonerCowerState : BasePrisonerState
{
    // 회전 속도 (높을수록 빨리 쳐다봄)
    private const float TurnSpeed = 5.0f;

    private static readonly int HitCowerTriggerHash = Animator.StringToHash("HitCower");

    public PrisonerCowerState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        // 1. 이동 멈춤
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        anim.SetTrigger(HitCowerTriggerHash); 

        // 플레이어 찾기 (부모 클래스 변수 활용)
        if (player == null)
        {
            var pObj = GameObject.FindWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
    }

    public override void Update()
    {
        // ★ [핵심] 플레이어 쪽 바라보기 로직
        if (player != null)
        {
            LookAtPlayer();
        }
    }

    private void LookAtPlayer()
    {
        // 1. 방향 벡터 계산
        Vector3 direction = player.position - fsm.transform.position;
        direction.y = 0; // 높낮이는 무시하고 수평 회전만

        // 2. 회전 실행
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            fsm.transform.rotation = Quaternion.Slerp(
                fsm.transform.rotation,
                targetRotation,
                Time.deltaTime * TurnSpeed
            );
        }
    }

    public override void Exit()
    {
        base.Exit();
    }

    // 맞고 있는 도중 또 맞았을 때 처리 (선택 사항)
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 3. 움찔하는 애니메이션 재생 (있다면)
        anim.SetTrigger(HitCowerTriggerHash);

        Debug.Log($"[{Controller.name}] 으악! (맞아서 웅크리기 시간 연장됨)");
    }
}