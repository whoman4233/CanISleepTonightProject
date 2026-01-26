using UnityEngine;

public class PrisonerCowerState : BasePrisonerState
{
    private float _recoverTimer = 0f;
    private const float CowerDuration = 5.0f;

    public PrisonerCowerState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        base.Enter();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }
        
        anim.SetTrigger("HitCower");
        anim.SetBool("Cower", true);

        _recoverTimer = CowerDuration;
        Debug.Log($"[{Controller.name}] 겁먹음(Cower) 상태 진입.");
    }

    public override void Update()
    {
        LookAtPlayer();

        _recoverTimer -= Time.deltaTime;

        // 상태 유지 확인 (혹시 다른 요인으로 꺼지는 것 방지)
        anim.SetBool("Cower", true);

        if (_recoverTimer <= 0f)
        {
            Recover();
        }
    }

    public override void Exit()
    {
        // 나갈 때 확실하게 끔
        anim.SetBool("Cower", false);
        base.Exit();
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 웅크린 상태에서 또 맞으면 시간 초기화 및 움찔
        _recoverTimer = CowerDuration;
        anim.SetTrigger("HitCower");
        Debug.Log($"[{Controller.name}] 으악! 또 때리지 마세요! (시간 연장)");
    }

    private void Recover()
    {
        fsm.ChangeState(fsm.ActionState);
    }

    private void LookAtPlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - fsm.transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            fsm.transform.rotation = Quaternion.Slerp(fsm.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }
    }
}