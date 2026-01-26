using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private float _actionLockTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackMotionTime = 0.5f; // 공격 모션 시간
    private const float HitStunTime = 0.5f;      // 피격 경직 시간
    private const float AttackRange = 1.5f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _cooldownTimer = 0.5f;

        // ★ [수정] 상태 진입 시 미세한 경직을 주어 이전 동작과의 충돌 방지
        _actionLockTimer = 0.1f;

        agent.isStopped = false;
        anim.SetBool("Run", true);

        // anim.SetBool("InCombat", true); // 필요 시 주석 해제
    }

    public override void Update()
    {
        if (player == null) return;

        // ★ [수정] 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // 공격 중인지, 혹은 "피격(Hit)" 중인지 확인 (Tag 설정 필수)
        bool isPlayingAttackAnim = stateInfo.IsTag("Attack");
        bool isPlayingHitAnim = stateInfo.IsTag("Hit");

        // 2. 행동 불가 상태 처리
        // 타이머가 남았거나, 공격 모션 중이거나, 맞고 있는 중이면 이동 금지
        if (_actionLockTimer > 0f || isPlayingAttackAnim || isPlayingHitAnim)
        {
            if (_actionLockTimer > 0f) _actionLockTimer -= Time.deltaTime;

            // ★ [중요] 이동 정지 (맞는 도중 미끄러짐 방지)
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // 공격/피격 중에도 플레이어를 바라보게 할지 결정 (여기선 바라봄)
            RotateTowardsPlayer();
            return;
        }

        // --- 여기서부터는 자유 행동 가능 ---

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // 사거리 내 도착 -> 정지 및 공격 준비
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);
            RotateTowardsPlayer();

            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            // 추격
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetBool("Run", true);
        }
    }

    private void Attack()
    {
        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool("HasWeapon", hasWeapon);

        int attackIndex = 0;
        int maxIndex = hasWeapon ? 2 : 3;
        attackIndex = Random.Range(0, maxIndex);

        // 파라미터 전달
        anim.SetFloat("AttackType", (float)attackIndex);
        anim.SetTrigger("Attack");

        _cooldownTimer = AttackCooldown;
        _actionLockTimer = AttackMotionTime; // 최소 경직 시간 부여
    }

    public override void Exit()
    {
        // anim.SetBool("InCombat", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 애니메이션 트리거
        anim.SetTrigger("Hit");
        _actionLockTimer = HitStunTime;

        // ★ [수정] 맞자마자 즉시 이동 멈춤
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
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