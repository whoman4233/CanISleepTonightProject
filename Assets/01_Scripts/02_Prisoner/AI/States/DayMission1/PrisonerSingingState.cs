using UnityEngine;

public class PrisonerSingingState : BasePrisonerState
{
    private float noiseTimer = 0f;

    public PrisonerSingingState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();
        // 노래 부르는 애니메이션 시작
        Anim.SetBool("IsSinging", true);

        // 노래 소리 재생 (Loop)
        // Controller.Sfx.PlaySingingLoop(); 
        Debug.Log($"{Controller.name}: 랄라라~ (노래 시작)");
    }

    public override void Update()
    {
        base.Update();

        // 주기적으로 소음(Noise) 이벤트 발생 (게임 매니저가 감지하도록)
        noiseTimer += Time.deltaTime;
        if (noiseTimer > 3.0f)
        {
            // 예: PrisonManager.Instance.ReportNoise(Controller.transform.position);
            noiseTimer = 0f;
        }
    }

    public override void Exit()
    {
        // 애니메이션 및 소리 종료
        Anim.SetBool("IsSinging", false);
        // Controller.Sfx.StopSingingLoop();
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 맞으면 노래를 멈추고 웅크림(Cower) 또는 전투(Combat) 상태로 전환
        Debug.Log("아악! 노래하는데 왜 때려!");
        fsm.ChangeState(fsm.CowerState);
    }
}