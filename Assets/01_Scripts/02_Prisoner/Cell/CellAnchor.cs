using System.Collections.Generic;
using UnityEngine;

public class CellAnchor : MonoBehaviour
{
    public string cellId;

    [Header("Spawns")]
    public Transform prisonerSpawn;
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

    // [추가] PrisonManager가 "빈 방"인지 체크할 때 사용합니다.
    [Header("Runtime - State")]
    public bool IsOccupied;

    /// <summary>
    /// 하루를 시작하기 전 기존 리스트 초기화
    /// </summary>
    public void ClearDailyAnomalies()
    {
        currentDailyAnomalies.Clear();
        // 하루 시작 시 점유 상태도 초기화하고 싶다면 아래 주석 해제
        // IsOccupied = false; 
    }

    /// <summary>
    /// 특정 종류(Kind)에 해당하는 오늘 배정된 이상현상들을 반환 (스포너에서 사용)
    /// </summary>
    public List<AnomalyDefinitionSO> GetAnomaliesByKind(AnomalyKind kind)
    {
        if (currentDailyAnomalies == null) return new List<AnomalyDefinitionSO>();
        return currentDailyAnomalies.FindAll(x => x.kind == kind);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 프리팹 작업 시 슬롯을 자동으로 끌어모으고 싶으면 사용
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