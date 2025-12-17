using UnityEngine;

public class PrisonerActor : MonoBehaviour
{
    // 내부 보관(기존 유지)
    public string instanceId { get; private set; }
    public string templateId { get; private set; }
    public PrisonerType type { get; private set; }

    public int hp { get; private set; }
    public int atk { get; private set; }
    public int spd { get; private set; }

    public bool IsAlive => hp > 0;

    private bool _combatEnabled;

    //SpawnController/다른 코드들이 기대하는 "PascalCase 호환 프로퍼티"
    public string InstanceId => instanceId;
    public string TemplateId => templateId;
    public PrisonerType Type => type;

    public int Hp => hp;
    public int Atk => atk;
    public int Spd => spd;

    public void Init(string instanceId, PrisonerDefinition def)
    {
        this.instanceId = instanceId;
        templateId = def.templateId;
        type = def.type;

        hp = def.hp;
        atk = def.atk;
        spd = def.spd;

        // 기본은 관찰 상태
        SetCombatEnabled(false);
    }

    public void TakeDamage(int dmg)
    {
        if (!IsAlive) return;
        if (!_combatEnabled) return;

        hp -= dmg;
        PrisonerEventBus.RaisePrisonerHit(instanceId, dmg);

        if (hp <= 0)
        {
            hp = 0;
            PrisonerEventBus.RaisePrisonerDown(instanceId);
            gameObject.SetActive(false);
        }
    }

    public void SetCombatEnabled(bool enabled)
    {
        _combatEnabled = enabled;

        // Bad AI는 전투 때만 활성
        var badAi = GetComponent<PrisonerBadAI>();
        if (badAi != null)
            badAi.enabled = enabled && type == PrisonerType.Bad;
    }
}
