public class DummyPrisonerRuntime
{
    public string InstanceId { get; }
    public string TemplateId { get; }
    public PrisonerType Type { get; }

    public int Hp { get; private set; }
    public int Atk { get; }
    public int Spd { get; }

    public bool IsAlive => Hp > 0;

    public DummyPrisonerRuntime(
        string instanceId,
        PrisonerDefinition def)
    {
        InstanceId = instanceId;
        TemplateId = def.templateId;
        Type = def.traitType;

        Hp = def.hp;
        Atk = def.atk;
        Spd = def.spd;
    }

    public void TakeDamage(int dmg)
    {
        if (!IsAlive) return;

        Hp -= dmg;
        PrisonerEventBus.RaisePrisonerHit(InstanceId, dmg);

        if (Hp <= 0)
        {
            Hp = 0;
            PrisonerEventBus.RaisePrisonerDown(InstanceId);
        }
    }
}
