using System.Collections.Generic;
using UnityEngine;

public class CellContentRegistry : MonoBehaviour
{
    public class CellContent
    {
        public PrisonerController prisoner;
        public readonly List<GameObject> anomalies = new();
        public string prisonerInstanceId; 
        public GameObject prop;
    }

    private readonly Dictionary<string, CellContent> _contentByCell = new();

    public bool TryGet(string cellId, out CellContent content) => _contentByCell.TryGetValue(cellId, out content);

    public void Set(string cellId, CellContent content) => _contentByCell[cellId] = content;

    public void ClearCell(string cellId)
    {
        if (!_contentByCell.TryGetValue(cellId, out var c)) return;

        if (c.prisoner != null) Destroy(c.prisoner.gameObject);
        foreach (var go in c.anomalies)
        {
            if (go != null) Destroy(go);
        }

        _contentByCell.Remove(cellId);
        if (c.prop != null) Destroy(c.prop);
    }

    public void ClearAll()
    {
        var keys = new List<string>(_contentByCell.Keys);
        foreach (var k in keys) ClearCell(k);
        _contentByCell.Clear();
    }
}
