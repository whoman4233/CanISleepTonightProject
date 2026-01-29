using UnityEngine;

public class PrisonerReturnState : BasePrisonerState
{
    // [추가] 끼임 감지를 위한 타이머
    private float _stuckTimer = 0f;

    public PrisonerReturnState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter(); // BasePrisonerState의 Enter 호출 (필요시)

        // 1. 목표 지점 (침대/스폰 위치)
        // [수정] SpawnPosition 대신 AssignedCell.prisonerSpawn 사용 (코드 요청사항 반영)
        Transform target = null;
        if (Controller.AssignedCell != null)
        {
            target = Controller.AssignedCell.prisonerSpawn;
        }

        // 스폰 위치도 없고, 할당된 방도 없으면 바로 일상 행동으로
        if (target == null)
        {
            fsm.ChangeState(fsm.ActionState);
            return;
        }

        // ================================================================
        // ★ [핵심 수정] 점검/행동 자세 강제 초기화
        // ================================================================
        // 이동하기 전에 기존의 특수 행동이나 점검 자세 파라미터를 끕니다.
        Controller.StopActionBehavior();

        // 점검 상태에서 켜졌을 수 있는 Suspicious나 기타 파라미터 초기화
        anim.SetBool("Suspicious", false);

        // [추가] Stuck 타이머 초기화
        _stuckTimer = 0f;

        float dist = Vector3.Distance(fsm.transform.position, target.position);
        if (dist > 0.5f)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);

                // ★ 이동 애니메이션 시작
                anim.SetBool("Walk", true);
            }
            else
            {
                Debug.LogWarning($"[ReturnState] {Controller.name} is not on NavMesh. Force transition.");
                fsm.ChangeState(fsm.ActionState);
            }
        }
        else
        {
            // 이미 제자리라면 바로 상태 전환
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Update()
    {
        if (agent == null || !agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        // 1. 정상적인 이동 완료 체크 (기존 로직)
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            fsm.ChangeState(fsm.ActionState);
            return;
        }

        // 2. [추가] 끼임(Stuck) 감지 로직
        // NavMeshAgent가 이동하려 하는데 속도가 거의 0이라면 (벽이나 다른 죄수에 막힘)
        if (agent.velocity.sqrMagnitude < 0.1f)
        {
            _stuckTimer += Time.deltaTime;

            // 2초 이상 제자리에 멈춰 있다면 도착한 것으로 간주하고 강제 상태 전이
            if (_stuckTimer > 2.0f)
            {
                // 필요하다면 로그 출력 (디버깅용)
                // Debug.LogWarning($"[ReturnState] {Controller.name} seems stuck. Force transition to Idle.");
                fsm.ChangeState(fsm.ActionState);
            }
        }
        else
        {
            // 움직이고 있다면 타이머 리셋
            _stuckTimer = 0f;
        }
    }

    public override void Exit()
    {
        // 나가면서 이동 애니메이션 끄기
        anim.SetBool("Walk", false);

        if (agent != null && agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // ================================================================
        // ★ [수정] 중앙화된 성향 판단 및 피격 모션 적용
        // ================================================================

        // 1. 공격적인 성향 (반격)
        if (Controller.IsAggressive)
        {
            anim.SetTrigger("Hit"); // 일반 피격 모션
            fsm.ChangeState(fsm.CombatState);
        }
        // 2. 소심한 성향 (겁먹음)
        else
        {
            anim.SetTrigger("HitCower"); // 웅크리는 피격 모션
            fsm.ChangeState(fsm.CowerState);
        }
    }
}