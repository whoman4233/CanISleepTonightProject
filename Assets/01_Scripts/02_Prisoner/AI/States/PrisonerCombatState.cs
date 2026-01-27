using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    // 쿨타임 및 사거리 설정
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 1.3f;

    // 공격 시작 후 애니메이션 태그가 인식되기까지 아주 짧은 유예 시간
    private float _attackTagDelayTimer = 0f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _cooldownTimer = 0.5f; // 진입 직후 잠깐 대기

        // ★ [핵심 1] Walk가 켜져 있으면 Run이 씹히므로 강제로 끕니다.
        anim.SetBool("Walk", false);
        anim.SetBool("Run", true);
        anim.SetBool("IsCombat", true);

        // 무기 장착
        if (fsm.Controller.HasWeapon)
        {
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
            fsm.Controller.StartActionBehavior(0);
        }

        // 이동 시작
        if (agent != null)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0.1f;
        }
    }

    public override void Update()
    {
        if (player == null) return;

        // 애니메이터 상태 정보 가져오기
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // 1. 피격 중이면 행동 불가
        if (stateInfo.IsTag("Hit"))
        {
            StopMovement();
            return;
        }

        // 2. 공격 중이면 행동 불가 (Tag 사용)
        // (_attackTagDelayTimer를 쓰는 이유: Trigger 발동 직후 Tag가 바뀌기 전 찰나의 순간에 이동해버리는 것 방지)
        if (_attackTagDelayTimer > 0f)
        {
            _attackTagDelayTimer -= Time.deltaTime;
            StopMovement();
            RotateTowardsPlayer(true); // 공격 초반 유도 성능
            return;
        }

        if (stateInfo.IsTag("Attack"))
        {
            StopMovement(); // 공격 중엔 확실히 멈춤

            // 공격 애니메이션 중에도 플레이어를 바라보게 할지 여부 (필요하면 주석 해제)
            // RotateTowardsPlayer(true); 
            return;
        }

        // --- 여기서부터 자유 행동 (추격/대기) ---

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // [사거리 안]
            StopMovement();
            RotateTowardsPlayer(true); // 플레이어 주시

            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            // [사거리 밖] -> 추격
            MoveToPlayer();
        }
    }

    private void Attack()
    {
        // 이동 정지
        StopMovement();

        // 무기 확인 및 애니메이션
        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool("HasWeapon", hasWeapon);

        int attackIndex = 0;
        if (hasWeapon && fsm.Controller.AIType == PrisonerAIType.Ambusher) attackIndex = 1;
        else if (!hasWeapon) attackIndex = Random.Range(0, 3);

        anim.SetFloat("AttackType", (float)attackIndex);
        anim.SetTrigger("Attack");

        // 쿨타임 리셋
        _cooldownTimer = AttackCooldown;

        // Trigger 발동 후 Tag가 "Attack"으로 바뀔 때까지 0.2초 정도 기다려줌 (미끄러짐 방지 핵심)
        _attackTagDelayTimer = 0.2f;
    }

    private void MoveToPlayer()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        // ★ [핵심 1] 이동 시에도 Walk를 확실히 꺼줘야 Run이 나옵니다.
        anim.SetBool("Walk", false);
        anim.SetBool("Run", true);

        RotateTowardsPlayer(false);
    }

    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        // 멈출 때는 Run 끄기
        anim.SetBool("Run", false);
    }

    public override void Exit()
    {
        anim.SetBool("IsCombat", false);
        anim.SetBool("Run", false);
        anim.SetBool("Walk", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit");
        StopMovement();
    }

    private void RotateTowardsPlayer(bool fastTurn)
    {
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            float speed = fastTurn ? 50f : 10f;
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }
    }
}