using UnityEngine;
using UnityEngine.AI;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private const float AttackCooldown = 1.5f;
    private const float AttackRange = 1.3f;
    private float _attackTagDelayTimer = 0f;

    // [최적화] 플레이어 찾는 빈도 조절용 타이머
    private float _playerFindTimer = 0f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        // 1. 플레이어 캐싱 안전장치
        if (player == null)
        {
            FindPlayer();
        }

        // 2. NavMeshAgent 심폐소생술 (위치 보정 및 활성화)
        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;

            // 위치가 어긋나 있다면 강제 동기화
            if (!agent.isOnNavMesh)
            {
                agent.Warp(fsm.transform.position);
            }

            agent.isStopped = false;
            agent.updatePosition = true;
            agent.updateRotation = true;

            // ★ [추가] 속도 초기화 (이전 상태에서 빨라졌을 수 있으므로)
            if (fsm.Controller.Data != null && fsm.Controller.Data.definition != null)
                agent.speed = fsm.Controller.Data.definition.spd;
            else
                agent.speed = 3.5f; // 기본값

            agent.stoppingDistance = 0.1f;
        }

        _cooldownTimer = 0.5f;

        // 3. 애니메이션 초기화 (이동 애니메이션은 Update에서 처리)
        anim.SetBool("Walk", false);
        anim.SetBool("IsCombat", true);

        // 4. 무기 장착
        if (fsm.Controller.HasWeapon)
        {
            // ★ [수정] 무기 타입만 설정하고, '0'으로 초기화하는 코드는 삭제함 (애니메이션 꼬임 방지)
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
        }
    }

    public override void Update()
    {
        // 1. 플레이어 유효성 검사 (없으면 주기적으로 재검색)
        if (player == null)
        {
            _playerFindTimer -= Time.deltaTime;
            if (_playerFindTimer <= 0f)
            {
                FindPlayer();
                _playerFindTimer = 1.0f; // 1초 뒤에 다시 찾기 (성능 보호)
            }

            if (player == null) // 여전히 없으면 정지
            {
                StopMovement();
                return;
            }
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // 2. 행동 불가 상태 체크 (피격, 공격 중)
        if (stateInfo.IsTag("Hit"))
        {
            StopMovement();
            return;
        }

        if (_attackTagDelayTimer > 0f)
        {
            _attackTagDelayTimer -= Time.deltaTime;
            StopMovement();
            RotateTowardsPlayer(true); // 공격 직전 유도력 보정
            return;
        }

        if (stateInfo.IsTag("Attack"))
        {
            StopMovement();
            return;
        }

        // 3. 전투 로직 수행
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // [사거리 안] 공격 시도
            StopMovement();
            RotateTowardsPlayer(true);

            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            // [사거리 밖] 추격
            MoveToPlayer();
        }
    }

    private void MoveToPlayer()
    {
        if (agent == null) return;

        // ★ [안전장치] Agent가 갑자기 꺼지거나 NavMesh에서 이탈했을 경우 복구 시도
        if (!agent.enabled) agent.enabled = true;
        if (!agent.isOnNavMesh) agent.Warp(fsm.transform.position);

        // 정상 상태일 때만 이동 및 Run 애니메이션
        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            anim.SetBool("Walk", false);
            anim.SetBool("Run", true);

            RotateTowardsPlayer(false);
        }
        else
        {
            // 복구 실패 시 멈춤 (제자리 걷기 방지)
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

        // 태그가 바뀌기 전까지 이동을 막아주는 버퍼 시간
        _attackTagDelayTimer = 0.2f;
    }

    private void StopMovement()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
        // ★ 멈출 때는 Run을 확실히 꺼야 함
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

    // 헬퍼: 플레이어 찾기
    private void FindPlayer()
    {
        var pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.transform;
    }
}