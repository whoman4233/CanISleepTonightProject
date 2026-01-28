using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 1.3f;
    private float _attackTagDelayTimer = 0f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        if (agent != null)
        {
            // 1. 꺼져있으면 켠다
            if (!agent.enabled)
                agent.enabled = true;

            // 2. NavMesh 위에 없으면(또는 방금 켜서 위치를 모르면) 현재 위치로 강제 이동(Warp)
            //    -> 이걸 해줘야 "제자리 달리기" 버그가 사라짐
            if (!agent.isOnNavMesh)
            {
                agent.Warp(fsm.transform.position);
            }

            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }
        _cooldownTimer = 0.5f;

        // [수정 1] 진입 시 무조건 Run을 켜지 않음. 
        // Update의 첫 프레임에서 거리를 재고 결정하도록 변경하여 꼬임 방지
        anim.SetBool("Walk", false);
        // anim.SetBool("Run", true); // <-- 이거 삭제 (Update에 맡김)
        anim.SetBool("IsCombat", true);

        // 무기 장착
        if (fsm.Controller.HasWeapon)
        {
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
            fsm.Controller.StartActionBehavior(0);
        }

        // Agent 설정 (아직 이동 명령은 내리지 않음)
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.stoppingDistance = 0.1f;
        }
    }

    public override void Update()
    {
        // [수정 2] 플레이어가 없으면 애니메이션 끄고 리턴 (안전장치)
        if (player == null)
        {
            StopMovement();
            return;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsTag("Hit"))
        {
            StopMovement();
            return;
        }

        if (_attackTagDelayTimer > 0f)
        {
            _attackTagDelayTimer -= Time.deltaTime;
            StopMovement();
            RotateTowardsPlayer(true);
            return;
        }

        if (stateInfo.IsTag("Attack"))
        {
            StopMovement();
            return;
        }

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // 사거리 안
            StopMovement(); // 여기서 Run = false가 됨
            RotateTowardsPlayer(true);

            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            // 사거리 밖 -> 추격
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        // [핵심 수정 3] Agent가 실제로 이동 가능한 상태일 때만 Run을 켭니다.
        // 재시작 직후 NavMesh 위에 없으면(isOnNavMesh == false) 이동 로직을 아예 건너뛰고 멈추게 해야 합니다.
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // 실제 이동 명령이 내려졌을 때만 애니메이션 실행
            anim.SetBool("Walk", false);
            anim.SetBool("Run", true);
            RotateTowardsPlayer(false);
        }
        else
        {
            // Agent가 고장났거나 NavMesh를 못 찾은 상태라면 강제로 멈춤
            // 이렇게 해야 제자리에서 뛰는 좀비 현상을 막을 수 있음
            StopMovement();

            // (선택) 필요하다면 여기서 바라보기만 시킴
            RotateTowardsPlayer(true);
        }
    }

    // (Attack, StopMovement, Exit, OnDamaged 등 나머지 메서드는 기존 유지)
    private void Attack()
    {
        StopMovement();

        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool("HasWeapon", hasWeapon);

        int attackIndex = 0;
        if (hasWeapon && fsm.Controller.AIType == PrisonerAIType.Ambusher) attackIndex = 1;
        else if (!hasWeapon) attackIndex = Random.Range(0, 3);

        anim.SetFloat("AttackType", (float)attackIndex);
        anim.SetTrigger("Attack");

        _cooldownTimer = AttackCooldown;
        _attackTagDelayTimer = 0.2f;
    }

    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        // 멈출 때는 Run 확실히 끄기
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
        if (player == null) return;
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            float speed = fastTurn ? 50f : 10f;
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }
    }
}