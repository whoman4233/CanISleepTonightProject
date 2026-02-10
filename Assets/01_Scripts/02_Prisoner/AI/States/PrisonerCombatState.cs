using UnityEngine;
using UnityEngine.AI;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 0.8f;
    private float _attackTagDelayTimer = 0f;

    // ★ [추가] 공격 시퀀스 제어용 플래그
    private bool _isAttackStarted = false;

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
        _isAttackStarted = false; // 진입 시 초기화

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

            // Stopping Distance를 사거리보다 약간 짧게 하여 공격 사거리 진입 보장
            agent.stoppingDistance = 0.6f;
        }

        _cooldownTimer = 0.2f;

        if (fsm.Controller.HasWeapon)
        {
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
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

        // 피격 상태면 이동 중지
        if (stateInfo.IsTag("Hit"))
        {
            _isAttackStarted = false; // 피격 시 공격 상태 리셋
            StopMovement();
            return;
        }

        // ★ 공격 애니메이션 재생 중이면 이동 차단 및 플래그 유지
        if (stateInfo.IsTag("Attack"))
        {
            StopMovement();
            _isAttackStarted = true;
            return;
        }

        // 공격 애니메이션이 끝났는데 플래그가 남아있다면 해제 (이제 이동 가능)
        if (_isAttackStarted && !stateInfo.IsTag("Attack"))
        {
            _isAttackStarted = false;
        }

        if (_attackTagDelayTimer > 0f)
        {
            _attackTagDelayTimer -= Time.deltaTime;
            StopMovement();
            RotateTowardsPlayer(true);
            return;
        }

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        // ★ [핵심 수정] 1회 공격 후 강제 추적 로직
        // 사거리 안이고, 쿨타임이 끝났으며, 현재 공격 중이 아닐 때만 공격 실행
        if (dist <= AttackRange && _cooldownTimer <= 0f && !_isAttackStarted)
        {
            Attack();
        }
        // 공격 중이 아니거나 사거리 밖이라면 즉시 플레이어 추적
        else if (!_isAttackStarted)
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
        _isAttackStarted = true; // 공격 시작 기록

        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool(HasWeaponHash, hasWeapon);

        int attackIndex = 0;
        if (hasWeapon && fsm.Controller.AIType == PrisonerAIType.Ambusher) attackIndex = 1;
        else if (!hasWeapon) attackIndex = Random.Range(0, 3);

        anim.SetFloat(AttackTypeHash, (float)attackIndex);
        fsm.Controller.PlayAttackSound();
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
        anim.SetBool(RunHash, false);
    }

    public override void Exit()
    {
        anim.SetBool(InCombatHash, false);
        anim.SetBool(RunHash, false);
        anim.SetBool(WalkHash, false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger(HitTriggerHash);
        _isAttackStarted = false; // 피격 시 시퀀스 초기화
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