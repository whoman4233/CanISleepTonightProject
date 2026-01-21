using UnityEngine;

public class PrisonerCowerState : BasePrisonerState
{
    private float _recoverTimer = 0f;
    private const float CowerDuration = 5.0f; // 5초 동안 겁먹음

    public PrisonerCowerState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        // 1. 이동 완전 정지
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        // 2. 애니메이션 설정
        // HitCower: 맞아서 움찔하는 모션 (Trigger)
        // Cower: 웅크리고 있는 루프 모션 (Bool) -> Animator에 추가 필요
        anim.SetTrigger("HitCower");
        anim.SetBool("Cower", true);

        // 3. 타이머 초기화
        _recoverTimer = CowerDuration;

        Debug.Log($"[{Controller.name}] 겁먹음(Cower) 상태 진입. 5초간 대기.");
    }

    public override void Update()
    {
        // 1. 플레이어 바라보기 (등 돌리고 있지 않게)
        LookAtPlayer();

        // 2. 시간 경과 체크
        _recoverTimer -= Time.deltaTime;
        if (_recoverTimer <= 0f)
        {
            Recover();
        }
    }

    public override void Exit()
    {
        // 나갈 때 웅크리기 해제
        anim.SetBool("Cower", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 겁먹은 상태에서 또 맞으면? -> 공포 시간 연장 & 움찔
        _recoverTimer = CowerDuration;
        anim.SetTrigger("HitCower");

        Debug.Log($"[{Controller.name}] 으악! 또 때리지 마세요! (시간 연장)");
    }

    private void Recover()
    {
        // 시간이 다 되면 다시 일반(Idle) 상태로 복귀
        // (점호 중이라면 InspectionState로 가야 하겠지만, 일단 기본 행동으로 복귀)
        fsm.ChangeState(fsm.ActionState);
    }

    private void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0; // Y축(위아래)은 보지 않음

        if (dir != Vector3.zero)
        {
            // 웅크린 채로 천천히 회전 (속도 5.0f)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }
}