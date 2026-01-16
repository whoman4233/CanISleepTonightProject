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
        switch (_currentStep)
        {
            case SubStep.StandUp:
                AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                // 일어나는 애니메이션이 끝났거나 일정 시간 지났으면 이동 시작
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
                    agent.isStopped = true; // 도착하면 정지
                }
                break;

            case SubStep.WaitAtPoint:
                LookAtPlayer();
                break;
        }
    }

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

    // 피격 시 이동을 즉시 멈추고 상태 전환
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (fsm == null || Controller == null) return;

        anim.SetTrigger("Hit");

        // [수정] 이동 즉시 정지 및 경로 초기화 (가장 중요)
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        // 상태 전환 로직
        if (IsAggressiveType(Controller.AIType))
        {
            fsm.ChangeState(fsm.CombatState);
        }
        else
        {
            fsm.ChangeState(fsm.CowerState);
        }
    }

    public override void Exit()
    {
        // 상태를 나갈 때 이동 애니메이션과 네비게이션을 확실히 멈춤
        anim.SetBool("Walk", false);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        base.Exit();
    }

    private bool IsAggressiveType(PrisonerAIType type)
    {
        // 공격적인 성향 리스트
        return type == PrisonerAIType.Bad ||
               type == PrisonerAIType.Ambusher ||
               type == PrisonerAIType.HammeringWall ||
               type == PrisonerAIType.Escaper;
    }
}