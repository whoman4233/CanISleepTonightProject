using UnityEngine;

public class PrisonerActor : MonoBehaviour
{
    public string instanceId { get; private set; }
    public string templateId { get; private set; }
    public PrisonerType type { get; private set; }

    public int hp { get; private set; }
    public int atk { get; private set; }
    public int spd { get; private set; }

    public bool IsAlive => hp > 0;

    public void Init(string instanceId, PrisonerDefinition def)
    {
        this.instanceId = instanceId;
        templateId = def.templateId;
        type = def.type;

        hp = def.hp;
        atk = def.atk;
        spd = def.spd;
    }

    public void TakeDamage(int dmg)
    {
        if (!IsAlive) return;

        hp -= dmg;
        PrisonerEventBus.RaisePrisonerHit(instanceId, dmg);

        if (hp <= 0)
        {
            hp = 0;
            PrisonerEventBus.RaisePrisonerDown(instanceId);

            // MVP: 바로 비활성 처리(나중에 래그돌/애니메이션)
            gameObject.SetActive(false);
        }
    }

    // 이런 느낌으로 사용
    //var ray = new Ray(cam.transform.position, cam.transform.forward);
    //    if (Physics.Raycast(ray, out var hit, range))
    //    {
    //        var actor = hit.collider.GetComponentInParent<PrisonerActor>();
    //        if (actor != null)
    //        {
    //            actor.TakeDamage(damage);
    //            Debug.Log($"[Baton] HIT {actor.instanceId} dmg={damage} hp={actor.hp}");
    //        }
    //    }
}
