using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerCowerState : BasePrisonerState
{
    public PrisonerCowerState(PrisonerFSM fsm) : base(fsm) { }

    public override void Enter()
    {
        agent.isStopped = true;
        anim.SetTrigger("Cower");
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        // 이미 웅크리고 있다면 더 벌벌 떨거나 소리 지르는 로직 추가 가능
        anim.SetTrigger("Cower_Heavy");
    }
}