using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    public List<string> GetAllCellIds()
    {
        // 내부적으로 Dictionary<string, CellAnchor> _anchors; 같은 자료구조를 쓴다면:
        return _byId.Keys.ToList();

        // 혹은 List<CellAnchor> _anchors 라면:
        // return _anchors.Select(a => a.cellId).ToList();
    }
}
