using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    // [수정 1] 타이머 분리: 쿨타임과 행동 불가(경직) 시간을 따로 관리해야 자연스럽습니다.
    private float _cooldownTimer = 0f;      // 다음 공격까지 남은 시간
    private float _actionLockTimer = 0f;    // 이동/회전이 불가능한 시간 (공격 모션 중, 피격 경직 등)

    private const float AttackCooldown = 1.5f;   // 공격 주기
    private const float AttackMotionTime = 0.6f; // 공격 애니메이션이 진행되는 동안 멈춰있을 시간 (애니 길이에 맞춰 조절)
    private const float HitStunTime = 0.5f;      // 피격 시 경직 시간

    // 공격 사거리 (PrisonerController에 정의되어 있다면 prisoner.AttackRange 사용 권장)
    private const float AttackRange = 1.5f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        // 진입 시 바로 공격하지 않고 약간의 딜레이
        _cooldownTimer = 0.5f;
        _actionLockTimer = 0f;

        agent.isStopped = false;
        anim.SetBool("Run", true);
    }

    public override void Update()
    {
        if (player == null) return;

        // 1. 타이머 갱신
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        if (_actionLockTimer > 0f) _actionLockTimer -= Time.deltaTime;

        // 2. [행동 불가 상태] 공격 모션 중이거나 맞아서 경직된 경우 -> 전면 정지
        if (_actionLockTimer > 0f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // (선택) 공격 중에는 회전 정도는 시킬지? 보통은 멈추는 게 자연스러움
            return;
        }

        // 3. 거리 계산
        float dist = Vector3.Distance(fsm.transform.position, player.position);

        // ================================================================
        // [공격 사거리 내부]
        // ================================================================
        if (dist <= AttackRange)
        {
            // 공격 위치 잡았으니 정지
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // 플레이어 바라보기 (공격 전 보정)
            RotateTowardsPlayer();

            // 쿨타임 끝났으면 공격 실행
            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        // ================================================================
        // [공격 사거리 밖] -> 추격
        // ================================================================
        else
        {
            // 경직(_actionLockTimer)이 풀렸다면 즉시 추격
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetBool("Run", true);
        }
    }

    private void Attack()
    {
        anim.SetTrigger("Attack");

        // [핵심] 쿨타임과 모션 정지 시간을 다르게 설정
        _cooldownTimer = AttackCooldown;       // 예: 1.5초 뒤에 다시 공격 가능
        _actionLockTimer = AttackMotionTime;   // 예: 0.6초 동안만 제자리에서 휘두르고, 그 뒤엔 이동 가능
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit");

        // 맞으면 잠시 멈춤 (경직)
        _actionLockTimer = HitStunTime;

        // (선택) 맞으면 공격 쿨타임도 살짝 밀어줄지? 
        // _cooldownTimer = Mathf.Max(_cooldownTimer, 0.2f);
    }

    private void RotateTowardsPlayer()
    {
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }
    }
}