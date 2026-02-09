using UnityEngine;

public class PrisonerInspectionState : BasePrisonerState
{
    private enum SubStep { StandUp, Moving, WaitAtPoint }
    private SubStep _currentStep;

    private static readonly int EnterCellTriggerHash = Animator.StringToHash("EnterCell");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int HitTriggerHash = Animator.StringToHash("Hit");
    private static readonly int HitCowerTriggerHash = Animator.StringToHash("HitCower");

    public PrisonerInspectionState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        _currentStep = SubStep.StandUp;
        anim.SetTrigger(EnterCellTriggerHash);

        // [안전장치] 플레이어 참조가 끊겼을 경우 다시 찾기
        if (player == null)
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
    }

    public override void Update()
    {
        // [추가] 항상 플레이어를 쳐다보게 할지 결정 (원하는 대로 주석 해제)
        // LookAtPlayer(); 

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
                // [수정] 도착 판정을 좀 더 너그럽게 (pathPending 체크 추가)
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                {
                    _currentStep = SubStep.WaitAtPoint;
                    anim.SetBool(WalkHash, false);
                    agent.isStopped = true;

                    // 도착 즉시 플레이어 방향으로 회전 강제
                    if (player != null)
                    {
                        Vector3 dir = (player.position - fsm.transform.position).normalized;
                        dir.y = 0;
                        if (dir != Vector3.zero) fsm.transform.rotation = Quaternion.LookRotation(dir);
                    }
                }
                break;

            case SubStep.WaitAtPoint:
                // [핵심] 여기서 플레이어를 계속 쳐다봄
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
        anim.SetBool(WalkHash, true);
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
    }

    // ... (OnDamaged 등 나머지 코드는 기존 유지)
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        if (fsm == null || Controller == null) return;
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
        if (Controller.IsAggressive)
        {
            anim.SetTrigger(HitTriggerHash);
            fsm.ChangeState(fsm.CombatState);
        }
        else
        {
            anim.SetTrigger(HitCowerTriggerHash);
            fsm.ChangeState(fsm.CowerState);
        }
    }

    public override void Exit()
    {
        anim.SetBool(WalkHash, false);
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        base.Exit();
    }
}