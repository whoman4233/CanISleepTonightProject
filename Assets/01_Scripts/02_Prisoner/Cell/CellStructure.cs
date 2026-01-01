using UnityEngine;
using System.Collections.Generic;

public class CellStructure : MonoBehaviour
{
    [Header("1. Structures")]
    public GameObject wallLeft;
    public GameObject wallRight;
    public GameObject wallFront;
    public GameObject floor;
    public GameObject steelBarred;
    public GameObject vent;

    // 내부 매핑용 딕셔너리
    private Dictionary<AnomalyTargetType, GameObject> _objectMap;

    private void Awake()
    {
        InitializeMap();
    }

    private void InitializeMap()
    {
        _objectMap = new Dictionary<AnomalyTargetType, GameObject>
        {
            { AnomalyTargetType.CellWall_Left,  wallLeft },
            { AnomalyTargetType.CellWall_Right, wallRight },
            { AnomalyTargetType.CellWall_Front, wallFront },
            { AnomalyTargetType.CellFloor,      floor },
            { AnomalyTargetType.SteelBarred,    steelBarred },
        };
    }

    public GameObject GetDefaultObject(AnomalyTargetType type)
    {
        if (_objectMap == null) InitializeMap();
        return _objectMap.TryGetValue(type, out var obj) ? obj : null;
    }

    // [청소용] 모든 가구를 다시 켜주는 함수
    public void ResetAllDefaults()
    {
        if (_objectMap == null) return;
        foreach (var obj in _objectMap.Values)
        {
            if (obj != null) obj.SetActive(true);
        }
    }
}