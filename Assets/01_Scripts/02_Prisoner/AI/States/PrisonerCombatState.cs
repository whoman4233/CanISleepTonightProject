using UnityEngine;
using UnityEngine.AI;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 1f;
    private float _attackTagDelayTimer = 0f;

    private bool _isAttackStarted = false;
    private float _playerFindTimer = 0f;

    // Animator Hashes
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
            agent.stoppingDistance = AttackRange * 0.9f;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.acceleration = 100f; // 가속도를 극단적으로 높여 즉각 정지 유도

            if (fsm.Controller.Data != null && fsm.Controller.Data.definition != null)
                agent.speed = fsm.Controller.Data.definition.spd;
            else
                agent.speed = 3.5f;
        }

        _cooldownTimer = 0.2f;
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

        // ★ [해결] 공격 중 미끄러짐 방지 핵심 강화
        // 애니메이션 태그가 "Attack"이거나 전이 중일 때도 물리 이동을 강제로 차단합니다.
        if (stateInfo.IsTag("Attack") || anim.IsInTransition(0))
        {
            ForceStopPhysicalMovement();
            _isAttackStarted = true;
            return;
        }

        // 공격 애니메이션이 끝난 시점 처리
        if (_isAttackStarted && !stateInfo.IsTag("Attack"))
        {
            _isAttackStarted = false;
        }

        if (_attackTagDelayTimer > 0f)
        {
            _attackTagDelayTimer -= Time.deltaTime;
            ForceStopPhysicalMovement(); // 경직 중에도 물리 이동 차단
            RotateTowardsPlayer(true);
            return;
        }

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

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

        if (currentDist <= agent.stoppingDistance + 0.1f)
        {
            ForceStopPhysicalMovement(); // 가까우면 즉시 정지
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
        // 공격 실행 로그 추가 (죄수 ID와 타겟 플레이어 정보 포함)
        Debug.Log($"<color=orange>[Combat] {fsm.Controller.Data.ID} : 플레이어를 공격합니다! (Distance: {Vector3.Distance(fsm.transform.position, player.position):F2})</color>");

        // 1. 물리 정지 및 애니메이션 파라미터 정리
        ForceStopPhysicalMovement();
        anim.SetBool(RunHash, false);
        anim.SetBool(WalkHash, false);

        _isAttackStarted = true;

        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool(HasWeaponHash, hasWeapon);

        int attackIndex = 0;
        if (hasWeapon && fsm.Controller.AIType == PrisonerAIType.Ambusher) attackIndex = 1;
        else if (!hasWeapon) attackIndex = Random.Range(0, 3);

        anim.SetFloat(AttackTypeHash, (float)attackIndex);
        fsm.Controller.PlayAttackSound();

        // 2. 트리거 실행
        anim.SetTrigger(AttackTriggerHash);

        _cooldownTimer = AttackCooldown;
        _attackTagDelayTimer = 0.3f;
    }

    private void ForceStopPhysicalMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // 물리 속도 0
            agent.ResetPath(); // 경로 데이터를 비워 관성 이동을 완전히 차단
        }
        anim.SetBool(RunHash, false);
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
        if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger(HitTriggerHash);
        _isAttackStarted = false;
        ForceStopPhysicalMovement();
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