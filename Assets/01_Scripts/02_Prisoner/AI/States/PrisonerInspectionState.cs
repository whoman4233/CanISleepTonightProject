using UnityEngine;

public class PrisonerInspectionState : BasePrisonerState
{
    private enum SubStep { StandUp, Moving, WaitAtPoint }
    private SubStep _currentStep;

    public PrisonerInspectionState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        _currentStep = SubStep.StandUp;
        anim.SetBool("IsSitting", false);
        anim.SetTrigger("StandUp");
    }

    public override void Update()
    {
        switch (_currentStep)
        {
            case SubStep.StandUp:
                // 애니메이션 이름 체크 혹은 일정 시간 후 이동
                if (anim.GetCurrentAnimatorStateInfo(0).IsTag("StandUpDone") ||
                    anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.9f)
                {
                    StartMoving();
                }
                break;

            case SubStep.Moving:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    _currentStep = SubStep.WaitAtPoint;
                    anim.SetBool("IsWalking", false);
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
        anim.SetBool("IsWalking", true);
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 점검 중 맞았을 때의 공통 반응 (애니메이션 등)
        anim.SetTrigger("Hit");

        // 전략 패턴: 죄수 타입에 따라 상태 전환
        if (actor.Type == PrisonerType.Bad)
            fsm.ChangeState(fsm.CombatState);
        else
            fsm.ChangeState(fsm.CowerState);
    }
}