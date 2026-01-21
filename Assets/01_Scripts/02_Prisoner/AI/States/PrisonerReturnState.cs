using UnityEngine;

public class PrisonerReturnState : BasePrisonerState
{
    public PrisonerReturnState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        // 1. 목표 지점 (침대/스폰 위치)
        Transform target = null;
        if (Controller.AssignedCell != null)
        {
            target = Controller.AssignedCell.prisonerSpawn;
        }

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

        // (만약 애니메이터에 'IsInspection' 같은 파라미터가 있다면 여기서 false로 꺼야 합니다)
        // anim.SetBool("IsInspection", false); 

        float dist = Vector3.Distance(fsm.transform.position, target.position);
        if (dist > 0.5f)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(target.position);

                // ★ 이동 애니메이션 시작 (이게 켜지면 점검 자세에서 전이되도록 Animator 설정 필요)
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
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Update()
    {
        if (!agent.isOnNavMesh || !agent.isActiveAndEnabled) return;

        // 이동 완료 체크
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            fsm.ChangeState(fsm.ActionState);
        }
    }

    public override void Exit()
    {
        // 나가면서 이동 애니메이션 끄기
        anim.SetBool("Walk", false);

        if (agent.isOnNavMesh && agent.isActiveAndEnabled)
        {
            agent.isStopped = true;
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

    // ★ 기존 private IsAggressive() 삭제됨 (Controller.IsAggressive 사용)
}