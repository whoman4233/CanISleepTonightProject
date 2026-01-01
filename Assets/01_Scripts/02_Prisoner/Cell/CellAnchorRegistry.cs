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

    // [추가] 모든 앵커 리스트 반환 (초기화 로직용)
    public IEnumerable<CellAnchor> GetAllAnchors()
    {
        // _anchorMap은 스크립트 상단에 선언된 Dictionary<string, CellAnchor> 변수 이름입니다.
        // 만약 이름이 _anchors 라면 return _anchors.Values; 로 바꿔주세요.
        return _byId.Values;
    }
}
