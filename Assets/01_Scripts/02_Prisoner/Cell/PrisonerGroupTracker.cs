using System.Collections.Generic;

public class PrisonerGroupTracker
{
    public string CellId { get; }
    private readonly HashSet<string> _alive = new();

    public PrisonerGroupTracker(string cellId, IEnumerable<string> prisonerIds)
    {
        CellId = cellId;
        foreach (var id in prisonerIds) _alive.Add(id);

        PrisonerEventBus.OnPrisonerDown += OnDown;
    }

    public void Dispose()
    {
        PrisonerEventBus.OnPrisonerDown -= OnDown;
        _alive.Clear();
    }

    private void OnDown(string prisonerId)
    {
        if (!_alive.Remove(prisonerId)) return;

        if (_alive.Count == 0)
            PrisonerEventBus.RaiseAllPrisonersDown(CellId);
    }
}
