using System.Collections.Generic;
using UnityEngine;

public class CellAnchorRegistry : MonoBehaviour
{
    private readonly Dictionary<string, CellAnchor> _byId = new();

    private void Awake()
    {
        _byId.Clear();
        foreach (var a in FindObjectsOfType<CellAnchor>(true))
        {
            if (string.IsNullOrWhiteSpace(a.cellId)) continue;
            _byId[a.cellId] = a;
        }
    }

    public bool TryGet(string cellId, out CellAnchor anchor) => _byId.TryGetValue(cellId, out anchor);
}
