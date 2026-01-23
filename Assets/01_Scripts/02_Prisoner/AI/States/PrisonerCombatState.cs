using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private float _actionLockTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackMotionTime = 0.5f; // 조금 넉넉하게 0.5초로 변경
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

        // ★ [추가] 전투 상태임을 알림 (애니메이터에서 '차려 자세' 대신 '전투 대기'를 틀게 하려면 필요)
        // 애니메이터에 Bool 파라미터 "InCombat"을 추가하고 Idle 조건을 수정하면 루프 문제 해결 가능
        // anim.SetBool("InCombat", true); 
    }

    public override void Update()
    {
        if (player == null) return;

        // 1. 공격 중인지 확인 (Tag 혹은 타이머)
        bool isPlayingAttackAnim = anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack");

        // 2. 행동 불가 상태 처리
        if (_actionLockTimer > 0f || isPlayingAttackAnim)
        {
            if (_actionLockTimer > 0f) _actionLockTimer -= Time.deltaTime;

            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // ★ [중요] 공격 중에도 회전은 되도록 하여 헛방 방지 (원치 않으면 주석 처리)
            RotateTowardsPlayer();
            return;
        }

        // --- 여기서부터는 자유 행동 가능 ---

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // 사거리 내 도착
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