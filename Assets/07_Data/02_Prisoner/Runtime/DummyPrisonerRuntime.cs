using UnityEngine;

public class DummyPrisonerRuntime
{
    public string PrisonerId { get; }
    public int Hp { get; private set; }

    public bool IsAlive => Hp > 0;

    public DummyPrisonerRuntime(string prisonerId, int hp)
    {
        PrisonerId = prisonerId;
        Hp = hp;
    }

    public void TakeDamage(int dmg)
    {
        if (!IsAlive) return;

        Hp -= dmg;
        PrisonerEventBus.RaisePrisonerHit(PrisonerId, dmg);

        if (Hp <= 0)
        {
            Hp = 0;
            PrisonerEventBus.RaisePrisonerDown(PrisonerId);
        }
    }
}
