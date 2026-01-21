using System.Collections.Generic;
using UnityEngine;

public class CellAnchor : MonoBehaviour
{
    public string cellId;

    [Header("Spawns")]
    public Transform prisonerSpawn; // 기존 메인 스폰 (기본값)

    // ★ [추가] 랜덤 스폰 포인트 리스트 (여기에 추가 지점들을 넣으세요)
    public List<Transform> randomSpawnPoints = new List<Transform>();

    public Transform inspectionPoint;
    public Transform propSpawnPoint;

    [Header("Anomaly Configuration")]
    [Tooltip("슬롯이 없거나 fallback이 필요할 때 사용되는 루트(비워도 됨)")]
    public Transform anomalyRoot;

    [Tooltip("감방 프리팹 안에 배치된 이상현상 스폰 포인트들(AnomalySpawnSlot)")]
    public List<AnomalySpawnSlot> anomalySlots = new();

    [Header("Runtime - Daily Assignment")]
    [Tooltip("매일 아침 AnomalyDistributor가 이 리스트를 채워줍니다. (공통 + 개별 + 특수)")]
    public List<AnomalyDefinitionSO> currentDailyAnomalies = new List<AnomalyDefinitionSO>();

    [Header("Runtime - State")]
    public bool IsOccupied;

    [Header("Refs")]
    public CellStructure structure;

    private void Awake()
    {
        if (structure == null) structure = GetComponentInChildren<CellStructure>();
    }

    public void ClearDailyAnomalies()
    {
        currentDailyAnomalies.Clear();
    }

    public List<AnomalyDefinitionSO> GetAnomaliesByKind(AnomalyKind kind)
    {
        if (currentDailyAnomalies == null) return new List<AnomalyDefinitionSO>();
        return currentDailyAnomalies.FindAll(x => x.kind == kind);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (anomalySlots == null) anomalySlots = new List<AnomalySpawnSlot>();
        anomalySlots.RemoveAll(x => x == null);

        var found = GetComponentsInChildren<AnomalySpawnSlot>(true);
        foreach (var s in found)
        {
            if (!anomalySlots.Contains(s))
                anomalySlots.Add(s);
        }
    }
#endif
}