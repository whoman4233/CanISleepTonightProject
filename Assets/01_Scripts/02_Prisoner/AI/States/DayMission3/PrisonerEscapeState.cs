using UnityEngine;

public class PrisonerEscapeState : BasePrisonerState
{
    public PrisonerEscapeState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 1. 문이 닫혀있다면? -> 문을 두드리거나 뚫고 나가는 연출 필요
        // 2. 문이 열려있다면? -> 감방 밖 웨이포인트(복도)로 전력 질주

        Anim.SetBool("IsRun", true);
        if (Agent != null && Agent.isOnNavMesh)
        {
            // 임시: 복도 중앙(0,0,0) 혹은 미리 지정된 탈출구로 이동
            Agent.SetDestination(new Vector3(0, 0, 0));
            Agent.speed = 3.5f; // 빠르게
        }
        Debug.Log($"[AI] {Controller.name}: 자유다!! (탈주 시도)");
    }

    public override void Exit()
    {
        Anim.SetBool("IsRun", false);
        if (Agent != null) Agent.ResetPath();
        base.Exit();
    }

    // 탈주범은 공격받으면 반격(Combat)
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir) => fsm.ChangeState(fsm.CombatState);
}