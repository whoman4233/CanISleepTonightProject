using System.Collections.Generic;
using UnityEngine;

public class CellAnchor : MonoBehaviour
{
    public string cellId;

    [Header("Spawns")]
    public Transform prisonerSpawn;
    public Transform inspectionPoint;
    public Transform playerEnterSpawn;
    public Transform playerExitSpawn;

    [Header("Anomaly")]
    [Tooltip("슬롯이 없거나 fallback이 필요할 때 사용되는 루트(비워도 됨)")]
    public Transform anomalyRoot;

    [Tooltip("감방 프리팹 안에 배치된 이상현상 스폰 포인트들(AnomalySpawnSlot)")]
    public List<AnomalySpawnSlot> anomalySlots = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 프리팹 작업 시 슬롯을 자동으로 끌어모으고 싶으면 사용(원치 않으면 제거)
        if (anomalySlots == null) anomalySlots = new List<AnomalySpawnSlot>();

        // null 제거
        anomalySlots.RemoveAll(x => x == null);

        // 자식에 슬롯이 있는데 리스트에 없다면 추가
        var found = GetComponentsInChildren<AnomalySpawnSlot>(true);
        foreach (var s in found)
        {
            if (!anomalySlots.Contains(s))
                anomalySlots.Add(s);
        }
    }
#endif
}
