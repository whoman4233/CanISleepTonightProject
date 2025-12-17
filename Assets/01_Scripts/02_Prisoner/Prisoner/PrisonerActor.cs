using UnityEngine;

public class PrisonerActor : MonoBehaviour
{
    public string InstanceId { get; private set; }
    public string CellId { get; private set; } 
    public PrisonerType Type { get; private set; }

    public int Hp { get; private set; }
    public int Atk { get; private set; }
    public int Spd { get; private set; }

    public bool IsAlive => Hp > 0;

    private bool _combatEnabled;

    public void Init(string cellId, string instanceId, PrisonerDefinition def)
    {
        CellId = cellId;
        InstanceId = instanceId;

        Type = def.type;
        Hp = def.hp;
        Atk = def.atk;
        Spd = def.spd;

        SetCombatEnabled(false);
    }

    public bool ApplyDamage(int dmg)
    {
        if (!IsAlive) return false;
        if (!_combatEnabled) return false;

        Hp -= dmg;
        PrisonerEventBus.RaisePrisonerHit(InstanceId, dmg);

        if (Hp <= 0)
        {
            Hp = 0;
            PrisonerEventBus.RaisePrisonerDown(InstanceId);
            gameObject.SetActive(false);
        }

        return true;
    }

    public void SetCombatEnabled(bool enabled)
    {
        _combatEnabled = enabled;

        var badAi = GetComponent<PrisonerBadAI>();
        if (badAi != null)
            badAi.enabled = enabled;
    }
}
