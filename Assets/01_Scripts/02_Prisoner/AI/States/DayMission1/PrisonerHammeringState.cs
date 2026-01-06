using UnityEngine;

public class PrisonerHammeringState : BasePrisonerState
{
    public PrisonerHammeringState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 망치 아이템 활성화 (Visual)
        // Controller.ShowHammer(true);

        Anim.SetBool("IsHammering", true);
        Debug.Log($"{Controller.name}: (벽을 쾅쾅 망치질 중)");
    }

    public override void Exit()
    {
        Anim.SetBool("IsHammering", false);
        // Controller.ShowHammer(false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 무기를 들고 있으므로 반격 가능성 높음
        fsm.ChangeState(fsm.CombatState);
    }
}