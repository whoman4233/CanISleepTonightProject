using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    private float _cooldownTimer = 0f;
    private float _actionLockTimer = 0f;

    // [수정 1] 멍때림 방지를 위해 쿨타임을 1.5f -> 1.0f로 단축
    private const float AttackCooldown = 1.0f;
    private const float AttackMotionTime = 0.5f; // 공격 모션 시간
    private const float HitStunTime = 0.5f;      // 피격 경직 시간
    private const float AttackRange = 1.5f;

    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        _cooldownTimer = 0.5f; // 진입 직후 약간의 딜레이

        // [수정] 상태 진입 시 미세한 경직을 주어 이전 동작과의 충돌 방지
        _actionLockTimer = 0.1f;

        // ★ [수정 2] 전투 진입 시 무기가 있다면 강제로 손에 쥐여줌 (투명 무기 방지)
        if (fsm.Controller.HasWeapon)
        {
            // 1. 해당 AI 타입에 맞는 도구(Prop) 활성화 (예: Ambusher의 단검)
            fsm.Controller.StartActionBehavior(fsm.Controller.AIType);
            // 2. 애니메이션 파라미터는 전투용(0)으로 초기화 (도구는 켜진 상태 유지)
            fsm.Controller.StartActionBehavior(0);
        }

        agent.isStopped = false;
        anim.SetBool("Run", true);
    }

    public override void Update()
    {
        if (player == null) return;

        // [수정] 애니메이션 상태 정보 가져오기
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // 공격 중인지, 혹은 "피격(Hit)" 중인지 확인 (Tag 설정 필수)
        bool isPlayingAttackAnim = stateInfo.IsTag("Attack");
        bool isPlayingHitAnim = stateInfo.IsTag("Hit");

        // 1. 행동 불가 상태 처리
        // 타이머가 남았거나, 공격 모션 중이거나, 맞고 있는 중이면 이동 금지
        if (_actionLockTimer > 0f || isPlayingAttackAnim || isPlayingHitAnim)
        {
            if (_actionLockTimer > 0f) _actionLockTimer -= Time.deltaTime;

            // [중요] 이동 정지 (맞는 도중 미끄러짐 방지)
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // [핵심 수정] 피격 중이 아닐 때(즉, 공격 중일 때)는 플레이어를 계속 바라봄
            // 맞을 때 몸이 돌아가면 어색하므로 피격 시엔 회전 금지
            if (!isPlayingHitAnim)
            {
                RotateTowardsPlayer(true); // fastTurn = true
            }
            return;
        }

        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;

        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= AttackRange)
        {
            // 2. 사거리 내 도착 -> 정지 및 공격 준비
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("Run", false);

            // [핵심 수정] 공격 대기(쿨타임) 중에도 플레이어를 계속 주시 (멍때림 방지)
            RotateTowardsPlayer(true);

            if (_cooldownTimer <= 0f)
            {
                Attack();
            }
        }
        else
        {
            // 3. 추격
            agent.isStopped = false;
            agent.SetDestination(player.position);
            anim.SetBool("Run", true);

            // 이동 중엔 부드럽게 회전
            RotateTowardsPlayer(false);
        }
    }

    private void Attack()
    {
        bool hasWeapon = fsm.Controller.HasWeapon;
        anim.SetBool("HasWeapon", hasWeapon);

        int attackIndex = 0;

        // ★ [수정 3] 무기 소지 여부 및 AI 타입에 따른 공격 모션 분기
        if (hasWeapon)
        {
            // [Case 1] 무기를 들고 있을 때
            if (fsm.Controller.AIType == PrisonerAIType.Ambusher)
            {
                attackIndex = 1; // Ambusher는 1번 (단검 찌르기 등)
            }
            else
            {
                attackIndex = 0; // 그 외 무기 사용자는 0번 (기본 휘두르기)
            }
        }
        else
        {
            // [Case 2] 무기가 없을 때 (맨손)
            // 0 ~ 2번 중 랜덤 발동 (Random.Range 정수형은 Max 제외이므로 3 입력)
            attackIndex = Random.Range(0, 3);
        }

        // 파라미터 전달
        anim.SetFloat("AttackType", (float)attackIndex);
        anim.SetTrigger("Attack");

        _cooldownTimer = AttackCooldown;
        _actionLockTimer = AttackMotionTime; // 최소 경직 시간 부여
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 피격 애니메이션 트리거
        anim.SetTrigger("Hit");
        _actionLockTimer = HitStunTime;

        // [수정] 맞자마자 즉시 이동 멈춤
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    // 회전 속도 파라미터(fastTurn) 추가
    private void RotateTowardsPlayer(bool fastTurn = false)
    {
        if (player == null) return;

        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            // 공격 시(fastTurn)에는 50의 속도로 거의 즉시 회전, 평소엔 10으로 부드럽게 회전
            float speed = fastTurn ? 50f : 10f;
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * speed);
        }
    }
}