using System.Collections.Generic;
using UnityEngine;

public class InspectionIndicatorManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspectionStateMachine;

    [Tooltip("모든 문(Door) 오브젝트들의 최상위 부모 (Mother Object)")]
    [SerializeField] private Transform doorRoot;

    [Header("Prefab")]
    [SerializeField] private InspectionIndicator indicatorPrefab; // 이펙트 프리팹
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 1.5f, 0.5f); // 문 위치 기준 오프셋

    // 생성된 인디케이터 관리 (CellId -> Indicator)
    private Dictionary<string, InspectionIndicator> _activeIndicators = new Dictionary<string, InspectionIndicator>();

    private void Start()
    {
        // 매니저 자동 할당
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (inspectionStateMachine == null) inspectionStateMachine = FindObjectOfType<InspectionStateMachine>();

        // 이벤트 구독
        EventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        if (inspectionStateMachine != null)
        {
            inspectionStateMachine.OnResolved += HandleCellResolved;
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
        if (inspectionStateMachine != null)
        {
            inspectionStateMachine.OnResolved -= HandleCellResolved;
        }
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Standby)
        {
            SpawnIndicatorsForActiveCells();
        }
        else if (e.Phase == GamePhase.Settlement)
        {
            ClearAllIndicators();
        }
    }

    private void HandleCellResolved(string cellId, bool isSuspicious, bool didSuppress)
    {
        if (_activeIndicators.TryGetValue(cellId, out var indicator))
        {
            if (indicator != null) Destroy(indicator.gameObject);
            _activeIndicators.Remove(cellId);
        }
    }

    private void SpawnIndicatorsForActiveCells()
    {
        ClearAllIndicators();

        if (cellManager == null || doorRoot == null)
        {
            Debug.LogWarning("[IndicatorManager] CellManager or DoorRoot is missing!");
            return;
        }

        foreach (var cell in cellManager.ActiveCells)
        {
            if (cell.WasResolvedToday) continue;

            // 1. CellID 파싱하여 문 오브젝트 찾기
            Transform targetDoor = FindDoorByCellId(cell.CellId);

            if (targetDoor != null)
            {
                // 2. 이펙트 생성 (문 위치 + 오프셋)
                // 문이 회전해있을 수 있으므로 rotation은 identity로 하거나, 문 회전을 따르게 설정
                var instance = Instantiate(indicatorPrefab, targetDoor.position + spawnOffset, Quaternion.identity);

                // 필요하다면 문을 부모로 설정 (문이 열릴 때 같이 움직이게 하려면)
                // instance.transform.SetParent(targetDoor); 

                _activeIndicators.Add(cell.CellId, instance);
            }
            else
            {
                Debug.LogWarning($"[IndicatorManager] Could not find door for CellID: {cell.CellId}");
            }
        }
    }

    /// <summary>
    /// CellID (예: C_1F_01)를 기반으로 Door Root 하위에서 문을 찾습니다.
    /// </summary>
    private Transform FindDoorByCellId(string cellId)
    {
        // 파싱 로직: C_1F_01 -> Door_1F_01 (이름 규칙에 맞게 수정 필요)
        // 예시: 단순히 "C_"를 "Door_"로 바꿔서 찾는 경우
        string doorName = "JailSlidingDoor_" + cellId;

        // 1. 직계 자식에서 찾기
        Transform door = doorRoot.Find(doorName);

        // 2. 만약 계층 구조가 복잡하다면(예: 1F > Door_01) 재귀적으로 찾기
        if (door == null)
        {
            door = FindDeepChild(doorRoot, doorName);
        }

        return door;
    }

    // 깊은 탐색 (재귀)
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void ClearAllIndicators()
    {
        foreach (var kvp in _activeIndicators)
        {
            if (kvp.Value != null) Destroy(kvp.Value.gameObject);
        }
        _activeIndicators.Clear();
    }
}