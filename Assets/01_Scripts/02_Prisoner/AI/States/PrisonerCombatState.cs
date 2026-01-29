using UnityEngine;
using UnityEngine.AI;

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

        // 1. 플레이어 찾기 (Base에 없거나 놓쳤을 경우 대비)
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        // 2. Agent 초기화 (Enter에서의 시도)
        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(fsm.transform.position);

            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;
        }

        _cooldownTimer = 0.5f;

        // 애니메이션 초기화 (Update에서 거리 재고 Run 켤 것임)
        anim.SetBool("Walk", false);
        anim.SetBool("IsCombat", true);

        // 무기 장착
        if (fsm.Controller.HasWeapon)
        {
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
            fsm.Controller.StartActionBehavior(0);
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.stoppingDistance = 0.1f;
        }
    }

    public override void Update()
    {
        // 플레이어가 없으면 다시 찾기 시도 (안 그러면 평생 멈춰있음)
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
            else
            {
                StopMovement();
                return;
            }
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
            StopMovement();
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
        if (agent == null) return;

        // Agent 복구 로직 강화
        // Agent가 꺼져있거나 NavMesh 위에 없으면 다시 살려내야 함
        if (!agent.enabled) agent.enabled = true;

        if (!agent.isOnNavMesh)
        {
            // Enter에서 실패했을 수 있으므로 여기서 재시도
            agent.Warp(fsm.transform.position);
        }

        // 복구 후 다시 체크: 이제 진짜 이동 가능한가?
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            anim.SetBool("Walk", false);
            anim.SetBool("Run", true); // 이동 성공 시 Run 켜기
            RotateTowardsPlayer(false);
        }
        else
        {
            // 여전히 복구 불가능하면 멈춤 (이때만 Standing 모션이 나옴)
            StopMovement();
            RotateTowardsPlayer(true);
        }
    }


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