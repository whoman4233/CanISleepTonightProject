using System;
using System.Collections.Generic;
using UnityEngine;

public class SettlementReportBuilder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementManager settlement;

    // =========================
    // Riot Gauge Cache (추가)
    // =========================
    private int _riotGaugeAtStart;

    private readonly List<ResolvedRecord> _resolved = new();
    private readonly HashSet<string> _resolvedIds = new();

    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<SettlementStartedEvent> _onSettlementStarted;
    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (settlement == null) settlement = FindObjectOfType<SettlementManager>();

        if (inspection != null)
            inspection.OnResolved += HandleResolved;

        _onPhaseChanged = e =>
        {
            if (e.Phase == GamePhase.Standby)
            {
                ClearResolvedCache();
                Debug.Log("SettlementReportBuilder의 ClearResolved 완료");
            }
        };
        _onSettlementStarted = OnSettlementStarted;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onSettlementStarted);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onSettlementStarted);
    }
    private void OnDestroy()
    {
        if (inspection != null)
            inspection.OnResolved -= HandleResolved;
    }

    // =========================
    // Riot Gauge Cache (추가)
    // =========================
    public void CacheRiotGaugeAtStart()
    {
        if (GameManager.Instance == null)
            return;

        _riotGaugeAtStart = GameManager.Instance.CurrentRiotGauge;

        Debug.Log($"[SettlementReportBuilder] RiotGauge cached = {_riotGaugeAtStart}");
    }
    private void OnSettlementStarted(SettlementStartedEvent e)
    {
        RunSettlement();
        Debug.Log("SettlementReportBuilder의 RunSettlement 완료");
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
        resolved = new List<ResolvedRecord>(_resolved);

        uninspected = new List<UninspectedRecord>();
        if (cellManager != null)
        {
            foreach (var cell in cellManager.Cells)
            {
                // [수정 전] if (cell.IsActiveToday) 
                // -> 점검을 했어도 ActiveToday는 true라서 계속 미점검으로 잡힘

                // [수정 후] 오늘 활성화된 방 중에서 + 아직 해결(Resolved)되지 않은 방만 체크
                if (cell.IsActiveToday && !cell.WasResolvedToday)
                {
                    uninspected.Add(new UninspectedRecord(cell.CellId, cell.IsSuspicious));
                }
            }
        }
    }

    public void ClearResolvedCache()
    {
        _resolved.Clear();
        _resolvedIds.Clear();
    }

    public void RunSettlement()
    {
        Debug.Log("[SettlementReportBuilder] RunSettlement START");
        // 1. 리스트 빌드
        BuildSettlementReport(out var resolved, out var uninspected);

        // 2. 게이지 등 게임 로직 반영
        settlement.ApplyDailyReport(resolved, uninspected);

        // 3. UI 표시용 데이터 생성
        SettlementUIData uiData = settlement.BuildSettlementData(resolved, uninspected);
        Debug.Log("[SettlementReportBuilder] Publish SettlementCompletedEvent");

        //Result UI Data 생성
        SettlementResultUIData resultUIData = BuildResultUIData(uiData);
        EventBus.Publish(new ResultUIShowRequestedEvent(resultUIData));
        EventBus.Publish(new SettlementCompletedEvent()); // 정산완료 알림(UI 버튼 연결)
        
        // Debug Log로 데이터 확인
        Debug.Log($"[Settlement UI Data] 1F Sus: {uiData.Floor1_AnomalyCount}, 2F Sus: {uiData.Floor2_AnomalyCount} | " +
                  $"Suppressed: {uiData.SuppressedCount}, Warned: {uiData.WarnedCount}, Unchecked: {uiData.UncheckedCount}");

        // TODO: 여기서 결과창 UI를 호출하며 uiData를 넘겨주면 됩니다.
        // 예: resultPanel.ShowResult(uiData);
        // 또는 EventBus를 통해 UI에 데이터를 발행할 수도 있습니다.
        // EventBus.Publish(new SettlementDataCreatedEvent(uiData));


    }

    private SettlementResultUIData BuildResultUIData(SettlementUIData uiData)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[SettlementReportBuilder] GameManager not found");
            return default;
        }

        int after = GameManager.Instance.CurrentRiotGauge;

        return new SettlementResultUIData
        {
            TotalAnomalyCount =
                uiData.Floor1_AnomalyCount +
                uiData.Floor2_AnomalyCount,

            SuppressedCount = uiData.SuppressedCount,
            WarnedCount = uiData.WarnedCount,
            UncheckedCount = uiData.UncheckedCount,

            RiotGaugeBefore = _riotGaugeAtStart,
            RiotGaugeAfter = after
        };
    }
}