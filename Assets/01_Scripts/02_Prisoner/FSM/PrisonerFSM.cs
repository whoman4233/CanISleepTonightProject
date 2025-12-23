using UnityEngine;
using UnityEngine.AI;

public class PrisonerFSM : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private PrisonerState currentState = PrisonerState.Idle;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float detectionRange = 5f;

    [Header("Refs")]
    private PrisonerActor actor;
    private NavMeshAgent agent;
    private Animator anim;
    private RagdollSetting ragdoll;
    private Transform playerTransform;

    private float attackTimer = 0f;

    private void Awake()
    {
        actor = GetComponent<PrisonerActor>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        ragdoll = GetComponent<RagdollSetting>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (currentState == PrisonerState.Dead) return;

        // 상태별 로직 실행
        switch (currentState)
        {
            case PrisonerState.Idle: UpdateIdle(); break;
            case PrisonerState.Inspection: UpdateInspection(); break;
            case PrisonerState.Combat: UpdateCombat(); break;
            case PrisonerState.Cower: UpdateCower(); break;
        }
    }

    // --- 상태 전환 함수 ---
    public void ChangeState(PrisonerState newState)
    {
        if (currentState == newState) return;

        // 이전 상태 종료 처리
        ExitState(currentState);

        currentState = newState;

        // 새로운 상태 시작 처리
        EnterState(newState);
    }

    private void EnterState(PrisonerState state)
    {
        switch (state)
        {
            case PrisonerState.Inspection:
                // 기획: 창살 앞으로 이동하는 모션
                anim.SetBool("IsStanding", true);
                break;
            case PrisonerState.Combat:
                if (agent != null) agent.isStopped = false;
                break;
            case PrisonerState.Cower:
                anim.SetTrigger("Cower"); // 기획: 웅크리기 자세
                if (agent != null) agent.isStopped = true;
                break;
            case PrisonerState.Dead:
                HandleDeath();
                break;
        }
    }

    private void ExitState(PrisonerState state) { /* 필요 시 작성 */ }

    // --- 상태별 루프 로직 ---
    private void UpdateIdle() { /* 침대에서 대기하는 로직 */ }

    private void UpdateInspection()
    {
        // 플레이어를 바라보게 함 (점호 자세)
        if (playerTransform != null)
        {
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }

    private void UpdateCombat()
    {
        if (playerTransform == null || actor.Type != PrisonerType.Bad) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= attackRange)
        {
            // 공격 범위 안이면 멈추고 공격
            agent.isStopped = true;
            attackTimer += Time.deltaTime;
            if (attackTimer >= 1.5f) // 공격 쿨타임
            {
                anim.SetTrigger("Attack");
                attackTimer = 0;
                // 플레이어 HP 깎는 이벤트 호출 가능
            }
        }
        else
        {
            // 공격 범위 밖이면 추격
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    private void UpdateCower() { /* 일정 시간 후 다시 Inspection으로 복귀하는 로직 등 */ }

    // --- 외부 호출 함수 (피격 등) ---
    public void OnDamaged(Vector3 hitPoint, Vector3 hitDir)
    {
        if (currentState == PrisonerState.Dead) return;

        // 1. 공통 피격 애니메이션
        anim.SetTrigger("Hit");

        // 2. 유형별 반응 분기 (기획 핵심)
        if (actor.Type == PrisonerType.Bad)
        {
            ChangeState(PrisonerState.Combat); // 반항형: 즉시 전투
        }
        else
        {
            ChangeState(PrisonerState.Cower);  // 순응형: 웅크리기
        }
    }

    private void HandleDeath()
    {
        if (agent != null) agent.enabled = false;
        if (ragdoll != null) ragdoll.ApplyImpact(transform.position, -transform.forward, 5f);
        this.enabled = false; // FSM 종료
    }
}