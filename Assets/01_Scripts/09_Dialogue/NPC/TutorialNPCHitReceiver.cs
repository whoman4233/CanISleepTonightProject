using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialNPCHitReceiver : PrisonerController
{
    private TutorialNPC npc;

    private void Awake()
    {
        npc = GetComponentInParent<TutorialNPC>();
    }

    public override bool ApplyDamage(int dmg, Vector3 hitPoint, Vector3 hitDirection) // 죄수컨트롤러에 있던 로직 오버라이드함
    {
        if (npc != null)
        {
            npc.OnAttacked();
        }
        Debug.Log("NPC가 공격받았습니다.");
        return true;
    }

    public void SimpleInit()
    {

    }
}
