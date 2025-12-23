using UnityEngine;

public class PrisonerDeadState : BasePrisonerState
{
    public PrisonerDeadState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        // 죽는 순간 한 번만 실행될 물리/컴포넌트 정리
        if (agent != null) agent.enabled = false;
        // 래그돌 처리는 Actor에서 직접 ApplyImpact를 호출하므로 여기선 상태만 유지
    }

    public override void Update() { } // 기능 없음
    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir) { } // 기능 없음
}