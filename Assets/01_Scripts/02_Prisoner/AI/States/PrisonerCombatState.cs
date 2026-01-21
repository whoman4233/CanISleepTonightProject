using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private float _actionLockTimer = 0f;

    private const float AttackCooldown = 1.5f;
    // [수정] 애니메이션 전환(Transition) 동안만 잡아둘 짧은 시간 (0.2초면 충분)
    private const float AttackMotionTime = 0.2f;
    private const float HitStunTime = 0.5f;
    private const float AttackRange = 1.5f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _cooldownTimer = 0.5f;
        _actionLockTimer = 0f;
        agent.isStopped = false;
        anim.SetBool("Run", true);

        Debug.Log($"[CombatState] Enter - PosY: {fsm.transform.position.y:F4}");
    }

    public override void Update()
    {
        if (player == null) return;

        // [디버그] 땅 파고드는 현상 감지
        if (fsm.transform.position.y < -0.1f)
        {
            Debug.LogWarning($"[CombatState] 땅으로 꺼짐 감지! Y: {fsm.transform.position.y:F4} | Time: {Time.time}");
        }

        // ================================================================
        // ★ [핵심] 공격 중 이동/회전 완전 봉쇄 로직
        // ================================================================
        // 1. 현재 재생 중인 상태가 'Attack' 태그를 가지고 있는지 확인
        bool isPlayingAttackAnim = anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

        // 2. 타이머가 남았거나 OR 공격 애니메이션 재생 중이면 -> 정지
        if (_actionLockTimer > 0f || isPlayingAttackAnim)
        {
            if (_actionLockTimer > 0f) _actionLockTimer -= Time.deltaTime;

            // 이동 정지
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // ★ 중요: 여기서 return해서 아래의 회전(RotateTowardsPlayer) 로직도 실행 안 되게 막음
            return;
        }

        // --- 아래는 이동/회전이 가능한 상태일 때만 실행됨 ---

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // 사거리 내 도달: 이동 멈춤 & 회전은 허용
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
            // 추격: 이동 허용
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

        // ================================================================
        // ★ [수정] 파라미터 이름 수정: "AttackIndex" -> "AttackType"
        // ================================================================
        anim.SetFloat("AttackType", (float)attackIndex);

        Debug.Log($"[CombatState] Attack! Idx: {attackIndex} (Set 'AttackType') | Weapon: {hasWeapon}");

        anim.SetTrigger("Attack");

        _cooldownTimer = AttackCooldown;

        // 애니메이션이 바뀌는 찰나(Transition)에는 Tag 인식이 안 될 수 있으므로
        // 아주 짧은 시간(0.2초) 동안 강제로 잠금
        _actionLockTimer = AttackMotionTime;
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit");
        // 피격 시에도 잠깐 멈추게 하려면 타이머 사용 (원하시면 값을 늘리세요)
        _actionLockTimer = HitStunTime;
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