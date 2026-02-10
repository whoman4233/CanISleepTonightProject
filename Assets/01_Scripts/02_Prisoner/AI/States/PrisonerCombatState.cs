using UnityEngine;
using UnityEngine.AI;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 0.8f;
    private float _attackTagDelayTimer = 0f;

    // 공격 시퀀스 제어용 플래그
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
        _isAttackStarted = false;

        anim.SetBool(InCombatHash, true);
        anim.SetBool(RunHash, true);

        if (player == null) FindPlayer();

        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(fsm.transform.position);

            agent.isStopped = false;
            // ★ [수정] 밀어내기 방지를 위해 정지 거리를 사거리와 유사하게 설정
            agent.stoppingDistance = AttackRange * 0.9f;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.acceleration = 60f; // 즉각적인 정지를 위해 가속도 상향

            if (fsm.Controller.Data != null && fsm.Controller.Data.definition != null)
                agent.speed = fsm.Controller.Data.definition.spd;
            else
                agent.speed = 3.5f;
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
            if (_playerFindTimer <= 0f) { FindPlayer(); _playerFindTimer = 1.0f; }
            if (player == null) { StopMovement(); return; }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // 피격 상태 처리
        if (stateInfo.IsTag("Hit"))
        {
            _isAttackStarted = false;
            StopMovement();
            return;
        }

        // ★ [해결] 공격 중 미끄러짐 방지: 애니메이션 재생 중엔 물리 속도를 강제로 0으로 고정
        if (stateInfo.IsTag("Attack"))
        {
            ForceStopPhysicalMovement();
            _isAttackStarted = true;
            return;
        }

        // 공격 애니메이션이 끝난 시점 처리
        if (_isAttackStarted && !stateInfo.IsTag("Attack"))
        {
            _isAttackStarted = false;
            // 공격 직후 이동 판단을 위해 쿨타임과 별개로 짧은 지연 부여 가능
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

        // ★ [해결] 2회 공격 방지: 쿨타임이 완전히 끝났고 공격 중이 아닐 때만 사거리 체크
        if (dist <= AttackRange && _cooldownTimer <= 0f && !_isAttackStarted)
        {
            Attack();
        }
        else if (!_isAttackStarted)
        {
            MoveToPlayer(dist);
        }
    }

    private void MoveToPlayer(float currentDist)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // ★ [해결] 플레이어 밀어내기 방지: 정지 거리 이내면 이동 중단
        if (currentDist <= agent.stoppingDistance + 0.1f)
        {
            StopMovement();
            RotateTowardsPlayer(true);
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);

        anim.SetBool(WalkHash, false);
        anim.SetBool(RunHash, true);

        RotateTowardsPlayer(false);
    }

    private void Attack()
    {
        // ★ 공격 시작 시 즉시 물리적 관성 제거
        ForceStopPhysicalMovement();
        _isAttackStarted = true;

        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool(HasWeaponHash, hasWeapon);

        int attackIndex = 0;
        if (hasWeapon && fsm.Controller.AIType == PrisonerAIType.Ambusher) attackIndex = 1;
        else if (!hasWeapon) attackIndex = Random.Range(0, 3);

        anim.SetFloat(AttackTypeHash, (float)attackIndex);
        fsm.Controller.PlayAttackSound();
        anim.SetTrigger(AttackTriggerHash);

        _cooldownTimer = AttackCooldown;
        _attackTagDelayTimer = 0.3f; // 공격 직후 짧은 경직
    }

    // ★ 미끄러짐 방지를 위한 물리 속도 즉시 제거 메서드
    private void ForceStopPhysicalMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // 물리 속도 즉시 0
        }
        anim.SetBool(RunHash, false);
    }

    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            // 부드러운 정지보다 미끄러짐 방지를 위해 속도를 빠르게 줄임
            agent.velocity = Vector3.Lerp(agent.velocity, Vector3.zero, Time.deltaTime * 10f);
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
        _isAttackStarted = false;
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