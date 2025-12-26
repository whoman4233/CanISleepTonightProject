using UnityEditor;
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

                // [디버깅용] 현재 재생 중인 애니메이션 상태 이름과 진행도를 콘솔에 출력
                // 문제 해결 후에는 주석 처리하세요.
                // Debug.Log($"Current State: {stateInfo.fullPathHash} / Time: {stateInfo.normalizedTime}");

                // ✅ 수정 포인트: 
                // 1. "Prisoner_Standing01" 대신 Animator 창에 있는 정확한 State 이름을 넣으세요. (예: "StandUp")
                // 2. Tag를 사용하는 것이 더 안전할 수도 있습니다. (예: stateInfo.IsTag("StandUp"))
                if (stateInfo.IsName("Prisoner_Standing01") && !anim.IsInTransition(0) && stateInfo.normalizedTime >= 0.95f)
                {
                    StartMoving();
                }
                // 혹시 State 이름이 Base Layer.Prisoner_Standing01 일 수도 있으니
                // 단순히 진행도로만 체크하는 안전장치를 추가하는 것도 방법입니다.
                // (단, Idle 상태가 아닐 때만)
                else if (!anim.IsInTransition(0) && stateInfo.IsTag("StandUpDone")) // 태그를 설정했다면 사용
                {
                    StartMoving();
                }
                break;

            case SubStep.Moving:
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    _currentStep = SubStep.WaitAtPoint;
                    anim.SetBool("Walk", false);
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