using UnityEngine;

public class PrisonerScreamingState : BasePrisonerState
{
    private float bangTimer = 0f;

    public PrisonerScreamingState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 문 앞(철장)으로 이동시키는 로직 필요 가능

        Anim.SetBool("IsScreaming", true);
        Debug.Log($"{Controller.name}: 내보내줘!!! (고함)");
    }

    public override void Update()
    {
        // 철장을 쾅쾅 때리는 타이밍에 맞춰 소음 발생
        bangTimer += Time.deltaTime;
        if (bangTimer > 2.0f)
        {
            // Controller.Sfx.PlayBangSound();
            bangTimer = 0f;
        }
    }

    public override void Exit()
    {
        Anim.SetBool("IsScreaming", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 흥분 상태이므로 바로 반격(Combat)하거나 제압(Cower)됨
        fsm.ChangeState(fsm.CombatState);
    }
}