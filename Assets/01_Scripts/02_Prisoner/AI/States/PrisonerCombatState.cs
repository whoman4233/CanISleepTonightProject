using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonerCombatState : BasePrisonerState
{
    public PrisonerCombatState(PrisonerFSM fsm) : base(fsm) { }

    public override void Update()
    {
        if (player == null) return;
        float dist = Vector3.Distance(fsm.transform.position, player.position);

        if (dist <= 1.5f) // 공격 사거리
        {
            agent.isStopped = true;
            anim.SetTrigger("Attack");
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    public override void OnDamaged(int damage, Vector3 hitPoint, Vector3 hitDir)
    {
        anim.SetTrigger("Hit"); // 싸우는 도중에도 피격 애니메이션은 재생
    }
}