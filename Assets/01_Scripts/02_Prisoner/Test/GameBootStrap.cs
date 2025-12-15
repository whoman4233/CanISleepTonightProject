using System.Collections.Generic;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementReportBuilder reportBuilder;
    [SerializeField] private SettlementManager settlement;

    [Header("Day Config")]
    [SerializeField] private int day = 0;
    [SerializeField] private int maxDays = 7;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode nextDayKey = KeyCode.F1;       // 하루 시작(Standby)
    [SerializeField] private KeyCode settlementKey = KeyCode.F8;     // 정산 실행(리포트 생성+게이지 반영)

    [Header("Auto Select Test Cell")]
    [SerializeField] private bool autoPickFirstActiveCell = true;

    [Header("Endings (MVP Text)")]
    [SerializeField] private bool stopGameOnEnding = true;
    private bool _ended;

    public string CurrentTestCellId { get; private set; }

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (reportBuilder == null) reportBuilder = FindObjectOfType<SettlementReportBuilder>();
        if (settlement == null) settlement = FindObjectOfType<SettlementManager>();

        cellManager?.BuildCellsIfNeeded();
    }

    private void Update()
    {
        if (Input.GetKeyDown(nextDayKey))
        {
            StartNextDay();
        }

        if (Input.GetKeyDown(settlementKey))
        {
            RunSettlement();
        }
    }

    public void StartNextDay()
    {
        if (_ended)
        {
            Debug.LogWarning("[Bootstrap] Already ended.");
            return;
        }

        if (settlement != null && settlement.IsRiotOver())
        {
            Debug.LogWarning("[Bootstrap] Riot already over. Stop.");
            return;
        }

        if (day >= maxDays)
        {
            Debug.Log("[Bootstrap] Reached max days. (End condition handled elsewhere)");
            return;
        }

        day++;
        Debug.Log($"================= DAY {day} START =================");

        settlement.ApplyDailyBaseIncrease();

        // 상승 직후 폭동 체크 (100 도달 즉시 게임오버 규칙)
        if (settlement.IsRiotOver())
        {
            Debug.LogWarning("[Ending] Bad Ending_산업 재해 (Standby Riot Over)");
            _ended = true;
            return;
        }

            // Standby
            cellManager.RunStandbySetup();

        // 리포트 캐시(Resolved 누적) 초기화: "하루 단위"로 쓰려면 이게 맞습니다.
        reportBuilder.ClearResolvedCache();

        // 테스트 편의: 첫 Active 방을 자동 선택
        if (autoPickFirstActiveCell)
        {
            var activeIds = cellManager.GetActiveCellIds();
            CurrentTestCellId = (activeIds.Count > 0) ? activeIds[0] : null;
            Debug.Log($"[Bootstrap] CurrentTestCellId = {CurrentTestCellId}");
        }

        DumpTodayActive();
    }

    public void RunSettlement()
    {
        if (reportBuilder == null || settlement == null)
        {
            Debug.LogWarning("[Bootstrap] Missing refs.");
            return;
        }

        reportBuilder.BuildSettlementReport(out List<ResolvedRecord> resolved, out List<UninspectedRecord> uninspected);

        Debug.Log($"[Bootstrap] SettlementReport Resolved={resolved.Count}, Uninspected={uninspected.Count}");
        settlement.ApplyDailyReport(resolved, uninspected);

        if (settlement.IsRiotOver())
        {
            Debug.LogWarning("[Bootstrap] GAME OVER: RiotGauge reached max.");
        }

        if (settlement.IsRiotOver())
        {
            Debug.LogWarning("[Ending] Bad Ending_산업 재해 (Settlement Riot Over)");
            _ended = true;
            return;
        }

        if (day >= maxDays)
        {
            // 7일차 정산 후 폭동이 안 났으면 엔딩
            Debug.Log("[Ending] Happy Ending_상태 유지 (7 days survived)");
            _ended = true;
            return;
        }
    }

    private void DumpTodayActive()
    {
        if (cellManager == null) return;

        Debug.Log("=== Today Active Cells ===");
        foreach (var id in cellManager.GetActiveCellIds())
        {
            var c = cellManager.GetCell(id);
            Debug.Log($"  - {c}");
        }
    }
}
