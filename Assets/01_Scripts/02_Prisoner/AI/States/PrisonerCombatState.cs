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

        // 1. 상태 파라미터 설정
        anim.SetBool(InCombatHash, true);

        if (player == null) FindPlayer();

        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;
            if (!agent.isOnNavMesh) agent.Warp(fsm.transform.position);

            agent.isStopped = false;
            agent.stoppingDistance = AttackRange * 0.8f;
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.acceleration = 100f;

            if (fsm.Controller.Data != null && fsm.Controller.Data.definition != null)
                agent.speed = fsm.Controller.Data.definition.spd;
            else
                agent.speed = 3.5f;
        }

        _cooldownTimer = 0.2f;

        // ★ [해결] 만약 플레이어가 멀리 있다면 즉시 이동 애니메이션 강제 적용
        float dist = Vector3.Distance(fsm.transform.position, player.position);
        if (dist > agent.stoppingDistance + 0.1f)
        {
            anim.SetBool(RunHash, true);
            // 애니메이터가 Idle을 거치지 않고 바로 Run으로 CrossFade 하게 유도
            anim.CrossFade("Run", 0.1f);
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

        // 공격 중일 때만 물리 이동을 강제로 차단 (전이 중 체크는 공격 시퀀스 시작 후에만 유효하도록 수정)
        if (stateInfo.IsTag("Attack"))
        {
            ForceStopPhysicalMovement();
            _isAttackStarted = true;
            return;
        }

        // 공격 애니메이션 종료 판단
        if (_isAttackStarted && !stateInfo.IsTag("Attack") && !anim.IsInTransition(0))
        {
            _isAttackStarted = false;
        }

        if (_attackTagDelayTimer > 0f)
        {
            _attackTagDelayTimer -= Time.deltaTime;
            ForceStopPhysicalMovement();
            RotateTowardsPlayer(true);
            return;
        }

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        // 공격 가능 거리라면 공격, 아니면 이동
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

        // 사거리보다 멀 때만 이동 로직 수행
        if (currentDist > agent.stoppingDistance + 0.05f)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            anim.SetBool(RunHash, true); // 여기서 확실히 Run을 켬
            RotateTowardsPlayer(false);
        }
        else
        {
            // 충분히 가까우면 정지하고 플레이어를 바라봄
            StopMovement();
            RotateTowardsPlayer(true);
        }
    }

    private void Attack()
    {
        Debug.Log($"<color=orange>[Combat] {fsm.Controller.Data.ID} : 플레이어를 공격합니다! (Distance: {Vector3.Distance(fsm.transform.position, player.position):F2})</color>");

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
        _attackTagDelayTimer = 0.3f;
    }

    private void ForceStopPhysicalMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
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