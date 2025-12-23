using System;
using System.Collections.Generic;
using UnityEngine;

public class SettlementReportBuilder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;

    private readonly List<ResolvedRecord> _resolved = new();
    private readonly HashSet<string> _resolvedIds = new();
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();

        if (inspection != null)
            inspection.OnResolved += HandleResolved;
        _onPhaseChanged = e =>
        {
            if (e.Phase == GamePhase.Standby)
            {
                ClearResolvedCache();
                Debug.Log("SettlementReportBuilder의 ClearResolved 완료");
            }
            else if(e.Phase == GamePhase.Settlement)
            {
                RunSettlement();
                Debug.Log("SettlementReportBuilder의 RunSettlement 완료");
            }
        };
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }
    private void OnDestroy()
    {
        if (inspection != null)
            inspection.OnResolved -= HandleResolved;
    }

    private void HandleResolved(string cellId, bool isSuspicious, bool didSuppress)
    {
        if (_resolvedIds.Contains(cellId)) return;
        _resolvedIds.Add(cellId);
        _resolved.Add(new ResolvedRecord(cellId, isSuspicious, didSuppress));
    }

    // 정산 페이즈 진입 순간 1회 호출
    public void BuildSettlementReport(out List<ResolvedRecord> resolved, out List<UninspectedRecord> uninspected)
    {
        // 1. 이미 완료되어 캐시에 쌓인 리스트 전달
        resolved = new List<ResolvedRecord>(_resolved);

        // 2. 미조사 리스트 생성
        uninspected = new List<UninspectedRecord>();
        if (cellManager != null)
        {
            foreach (var cell in cellManager.Cells)
            {
                // ✅ 수정된 조건: 오늘 활성 방(IsActiveToday)이면서 
                // ✅ 동시에 이미 조사 완료된 ID 목록(_resolvedIds)에 없어야만 "미점검"입니다.
                if (cell.IsActiveToday && !_resolvedIds.Contains(cell.CellId))
                {
                    uninspected.Add(new UninspectedRecord(cell.CellId, cell.IsSuspicious));
                }
            }
        }
    }

    // 다음날을 위해 초기화(필요 시 호출)
    public void ClearResolvedCache()
    {
        _resolved.Clear();
        _resolvedIds.Clear();
    }

    [SerializeField] private SettlementManager settlement;

    public void RunSettlement()
    {
        BuildSettlementReport(out var resolved, out var uninspected);
        settlement.ApplyDailyReport(resolved, uninspected);

        if (settlement.IsRiotOver())
        {
            Debug.Log("[GameOver] Riot occurred");
            // 엔딩 트리거
            if(GameManager.Instance.CurrentDay < 7)
            {
                EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding2)); // 산업 재해(7일 이전에 폭동 100 이상)
                Debug.Log("BadEnding2");
            }
            else
            {
                EventBus.Publish(new EndingConditionMetEvent(GameEndingType.BadEnding3)); // 위기 회피(7일차에 폭동 100 이상으로 퇴근)
                Debug.Log("BadEnding3");
            }
        }
    }

}
