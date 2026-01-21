using UnityEngine;

public class PrisonerInspectionState : BasePrisonerState
{
    private enum SubStep { StandUp, Moving, WaitAtPoint }
    private SubStep _currentStep;

    public PrisonerInspectionState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        _currentStep = SubStep.StandUp;
        anim.SetBool("Suspicious", false);
        anim.SetTrigger("EnterCell");
    }

    public override void Update()
    {
        // ... (기존 Update 로직 유지: StandUp, Moving, WaitAtPoint) ...
        switch (_currentStep)
        {
            case SubStep.StandUp:
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Prisoner_Standing01") && !anim.IsInTransition(0) && stateInfo.normalizedTime >= 0.1f)
                {
                    StartMoving();
                }
                break;

            case SubStep.Moving:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    _currentStep = SubStep.WaitAtPoint;
                    anim.SetBool("Walk", false);
                    agent.isStopped = true;
                }
                break;

            case SubStep.WaitAtPoint:
                LookAtPlayer();
                break;
        }
    }

    // ... (StartMoving, LookAtPlayer 유지) ...
    private void StartMoving()
    {
        if (fsm.InspectionPoint == null) return;
        _currentStep = SubStep.Moving;
        agent.isStopped = false;
        agent.SetDestination(fsm.InspectionPoint.position);
        anim.SetBool("Walk", true);
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }


    // ================================================================
    // ★ [수정] 피격 로직 통합 및 간소화
    // ================================================================
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (fsm == null || Controller == null) return;

        // 1. 이동 즉시 정지 (공통)
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        // 2. 성향에 따른 분기 (Controller.IsAggressive 사용)
        if (Controller.IsAggressive)
        {
            // 공격적: 일반 피격 모션 -> 전투 태세
            anim.SetTrigger("Hit");
            Debug.Log($"[{Controller.name}] 점호 중 피격! 반격합니다.");
            fsm.ChangeState(fsm.CombatState);
        }
        else
        {
            // 소심함: 겁쟁이 피격 모션(HitCower) -> 겁먹음 상태
            anim.SetTrigger("HitCower");
            Debug.Log($"[{Controller.name}] 점호 중 피격! 겁을 먹습니다.");
            fsm.ChangeState(fsm.CowerState);
        }
    }

    public override void Exit()
    {
        anim.SetBool("Walk", false);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        base.Exit();
    }

    // ★ 기존에 있던 private bool IsAggressiveType(...) 삭제됨
}