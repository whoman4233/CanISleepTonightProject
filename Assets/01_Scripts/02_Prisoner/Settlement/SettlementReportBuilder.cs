using System;
using System.Collections.Generic;
using UnityEngine;

public class SettlementReportBuilder : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonManager prisonManager; // 변수명 변경 (cellManager -> prisonManager)
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementManager settlement;

    // Riot Gauge Cache
    private int _riotGaugeAtStart;

    private readonly List<ResolvedRecord> _resolved = new();
    private readonly HashSet<string> _resolvedIds = new();

    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<SettlementStartedEvent> _onSettlementStarted;

    private void Awake()
    {
        if (prisonManager == null) prisonManager = FindObjectOfType<PrisonManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (settlement == null) settlement = FindObjectOfType<SettlementManager>();

        if (inspection != null)
            inspection.OnResolved += HandleResolved;

        _onPhaseChanged = e =>
        {
            if (e.Phase == GamePhase.Standby)
            {
                ClearResolvedCache();
                // 하루 시작 시점의 게이지를 캐싱해둠 (정산 시 Before 값으로 쓰기 위해)
                CacheRiotGaugeAtStart();
                Debug.Log("[ReportBuilder] New Day Started. Cache Cleared & Gauge Cached.");
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
    // Riot Gauge Cache
    // =========================
    public void CacheRiotGaugeAtStart()
    {
        if (GameManager.Instance == null) return;

        // [수정] CurrentRiotGauge -> RiotGauge (GameManager 프로퍼티명 확인 필요)
        _riotGaugeAtStart = GameManager.Instance.RiotGauge;
        Debug.Log($"[ReportBuilder] Gauge Cached: {_riotGaugeAtStart}");
    }

    private void OnSettlementStarted(SettlementStartedEvent e)
    {
        RunSettlement();
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

        // [수정] PrisonManager 접근 방식 변경
        // PrisonManager 내부의 Cells 리스트가 public인지, 아니면 별도 접근자가 있는지 확인
        // 만약 _runtimeCells가 private라면 GetActiveCellIds() 등을 활용해야 함.

        // PrisonManager가 Cells 프로퍼티를 제공한다고 가정 (없으면 추가 필요)
        // public IReadOnlyList<CellRuntime> Cells => _runtimeCells.Values.ToList(); 같은 형태

        // 만약 Cells 접근이 어렵다면 GetActiveCellIds()를 활용하여 순회
        if (prisonManager != null)
        {
            // 여기서는 PrisonManager가 Cells 리스트를 제공하지 않을 경우를 대비해 
            // GetActiveCellIds를 이용해 역으로 데이터를 찾거나, 
            // PrisonManager에 public 접근자를 만들어야 함.

            // PrisonManager에 public IEnumerable<CellRuntime> AllCells => _runtimeCells.Values; 추가 추천

            // 일단 기존 코드 유지 (에러 나면 PrisonManager에 프로퍼티 추가하세요)
            // foreach (var cell in prisonManager.Cells) ...
        }
    }

    // [보완] PrisonManager.cs에 추가할 프로퍼티 (없다면)
    // public IEnumerable<CellRuntime> Cells => _runtimeCells.Values;

    public void ClearResolvedCache()
    {
        _resolved.Clear();
        _resolvedIds.Clear();
    }

    public void RunSettlement()
    {
        Debug.Log("[ReportBuilder] RunSettlement START");

        // 1. 리스트 빌드 (미점검 방 계산)
        // 여기서는 PrisonManager를 통해 미점검 방을 찾아야 하므로
        // BuildSettlementReport 로직을 아래와 같이 구체화합니다.

        var resolvedList = new List<ResolvedRecord>(_resolved);
        var uninspectedList = new List<UninspectedRecord>();

        if (prisonManager != null)
        {
            // PrisonManager에 GetCellRuntime 메서드가 있으므로 활용
            var activeIds = prisonManager.GetActiveCellIds();
            foreach (var id in activeIds)
            {
                var cell = prisonManager.GetCell(id); // Helper 메서드 활용
                if (cell != null && !cell.WasResolvedToday)
                {
                    uninspectedList.Add(new UninspectedRecord(cell.CellId, cell.IsSuspicious));
                }
            }
        }

        // 2. 게이지 등 게임 로직 반영
        settlement.ApplyDailyReport(resolvedList, uninspectedList);

        // 3. UI 표시용 데이터 생성
        SettlementUIData uiData = settlement.BuildSettlementData(resolvedList, uninspectedList);

        // 4. Result UI Data 생성 및 이벤트 발행
        SettlementResultUIData resultUIData = BuildResultUIData(uiData);
        EventBus.Publish(new ResultUIShowRequestedEvent(resultUIData));
        EventBus.Publish(new SettlementCompletedEvent());
    }

    private SettlementResultUIData BuildResultUIData(SettlementUIData uiData)
    {
        if (GameManager.Instance == null) return default;

        int after = GameManager.Instance.RiotGauge; // 프로퍼티명 통일

        return new SettlementResultUIData
        {
            TotalAnomalyCount = uiData.Floor1_ActiveCount + uiData.Floor2_ActiveCount,
            SuppressedCount = uiData.SuppressedCount,
            WarnedCount = uiData.WarnedCount,
            UncheckedCount = uiData.UncheckedCount,
            RiotGaugeBefore = _riotGaugeAtStart,
            RiotGaugeAfter = after
        };
    }
}