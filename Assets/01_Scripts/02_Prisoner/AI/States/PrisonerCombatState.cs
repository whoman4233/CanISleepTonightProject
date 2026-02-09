using UnityEngine;
using UnityEngine.AI;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 0.8f;
    private float _attackTagDelayTimer = 0f;

    // [최적화] 플레이어 찾는 빈도 조절용 타이머
    private float _playerFindTimer = 0f;

    // ================================================================
    // Animator Hashes 캐싱
    // ================================================================
    private static readonly int InCombatHash = Animator.StringToHash("InCombat");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int HasWeaponHash = Animator.StringToHash("HasWeapon");
    private static readonly int AttackTypeHash = Animator.StringToHash("AttackType");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int HitTriggerHash = Animator.StringToHash("Hit");

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        anim.SetBool(InCombatHash, true);

        anim.CrossFade("Run", 0.1f);
        anim.SetBool(RunHash, true);

        if (player == null) FindPlayer();

        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(fsm.transform.position);

            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;

            if (fsm.Controller.Data != null && fsm.Controller.Data.definition != null)
                agent.speed = fsm.Controller.Data.definition.spd;
            else
                agent.speed = 3.5f;

            agent.stoppingDistance = AttackRange * 0.8f;
        }

        _cooldownTimer = 0.2f;

        if (fsm.Controller.HasWeapon)
        {
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
        }

        if (player != null)
        {
            float dist = Vector3.Distance(fsm.transform.position, player.position);

            if (dist > AttackRange)
            {
                MoveToPlayer();
            }
            else
            {
                RotateTowardsPlayer(true);
            }
        }
    }

    public override void Update()
    {
        if (player == null)
        {
            _playerFindTimer -= Time.deltaTime;
            if (_playerFindTimer <= 0f)
            {
                FindPlayer();
                _playerFindTimer = 1.0f;
            }

            if (player == null)
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
            StopMovement();
            RotateTowardsPlayer(true);

            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        if (agent == null) return;

        if (!agent.enabled) agent.enabled = true;
        if (!agent.isOnNavMesh) agent.Warp(fsm.transform.position);

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            // [수정] Hash 사용
            anim.SetBool(WalkHash, false);
            anim.SetBool(RunHash, true);

            RotateTowardsPlayer(false);
        }
        else
        {
            StopMovement();
            RotateTowardsPlayer(true);
        }
    }

    private void Attack()
    {
        StopMovement();

        bool hasWeapon = fsm.Controller.HasWeapon;
        // [수정] Hash 사용
        anim.SetBool(HasWeaponHash, hasWeapon);

        int attackIndex = 0;
        if (hasWeapon && fsm.Controller.AIType == PrisonerAIType.Ambusher) attackIndex = 1;
        else if (!hasWeapon) attackIndex = Random.Range(0, 3);

        // [수정] Hash 사용
        anim.SetFloat(AttackTypeHash, (float)attackIndex);

        fsm.Controller.PlayAttackSound();

        // [수정] Hash 사용
        anim.SetTrigger(AttackTriggerHash);

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
        // [수정] Hash 사용
        anim.SetBool(RunHash, false);
    }

    public override void Exit()
    {
        // [수정] Hash 사용
        anim.SetBool(InCombatHash, false);
        anim.SetBool(RunHash, false);
        anim.SetBool(WalkHash, false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // [수정] Hash 사용
        anim.SetTrigger(HitTriggerHash);
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

    private void FindPlayer()
    {
        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
    }
}