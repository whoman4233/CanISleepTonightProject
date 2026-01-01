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
                if (stateInfo.IsName("Prisoner_Standing01") && !anim.IsInTransition(0) && stateInfo.normalizedTime >= 0.1f)
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
        // 👇 null 체크를 강화하여 로그를 띄웁니다.
        if (fsm.InspectionPoint == null)
        {
            Debug.LogError($"[InspectionState] 이동 실패: {fsm.name}의 InspectionPoint가 null입니다! (Controller.Initialize 확인 필요)");
            return;
        }

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
        // [안전장치 1] FSM 참조가 있는지 확인
        if (fsm == null)
        {
            Debug.LogError("[InspectionState] OnDamaged: fsm is null!");
            return;
        }

        // 점검 중 맞았을 때의 공통 반응 (애니메이션 등)
        anim.SetTrigger("Hit");

        // [안전장치 2] 컨트롤러 참조가 있는지 확인 (AIType 접근 전)
        if (Controller == null)
        {
            Debug.LogError("[InspectionState] OnDamaged: Controller is null!");
            // 컨트롤러가 없어도 일단 전투 상태로라도 보내는 게 나을 수 있음 (선택 사항)
            if (fsm.CombatState != null) fsm.ChangeState(fsm.CombatState);
            return;
        }

        // 전략 패턴: 죄수 타입에 따라 상태 전환
        if (Controller.AIType == PrisonerAIType.Bad)
        {
            // [안전장치 3] 전환할 상태가 존재하는지 확인
            if (fsm.CombatState != null)
                fsm.ChangeState(fsm.CombatState);
            else
                Debug.LogError("[InspectionState] CombatState is null!");
        }
        else
        {
            if (fsm.CowerState != null)
                fsm.ChangeState(fsm.CowerState);
            else
                Debug.LogError("[InspectionState] CowerState is null!");
        }
    }
}