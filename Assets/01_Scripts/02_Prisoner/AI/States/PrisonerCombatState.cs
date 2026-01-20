using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private float _actionLockTimer = 0f;

    private const float AttackCooldown = 1.5f;
    private const float AttackMotionTime = 0.6f;
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
    }

    public override void Update()
    {
        if (player == null) return;

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        if (_actionLockTimer > 0f) _actionLockTimer -= Time.deltaTime;

        if (_actionLockTimer > 0f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);
            return;
        }

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
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
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetBool("Run", true);
        }
    }

    // ================================================================
    // ★ [수정] 무기 및 특수 타입에 따른 모션 분기 로직 적용
    // ================================================================
    private void Attack()
    {
        // 1. 무기 소지 여부 (Controller의 AIType 기반 판단)
        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool("HasWeapon", hasWeapon);

        // 2. 공격 모션 인덱스 결정
        int attackIndex = 0;

        if (fsm.Controller.IsSpecialAttacker)
        {
            // 특수 개체는 고정된 인덱스 (예: 0번 모션)
            attackIndex = 0;
        }
        else
        {
            // 일반 개체: 무기가 있으면 2개(0,1), 없으면 3개(0,1,2) 중 랜덤
            int maxIndex = hasWeapon ? 2 : 3;
            attackIndex = Random.Range(0, maxIndex);
        }

        // 3. 인덱스 전달
        anim.SetInteger("AttackIndex", attackIndex);

        // 4. 공격 트리거 발동
        anim.SetTrigger("Attack");

        _cooldownTimer = AttackCooldown;
        _actionLockTimer = AttackMotionTime;
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit");
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