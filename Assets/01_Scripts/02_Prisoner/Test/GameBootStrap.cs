using System.Collections.Generic;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementReportBuilder reportBuilder;
    [SerializeField] private SettlementManager settlement;
    [SerializeField] private PrisonerSpawnController spawner; // ✅ 추가

    [Header("Day Config")]
    [SerializeField] private int day = 0;
    [SerializeField] private int maxDays = 7;

    [Header("Debug Keys")]
    [SerializeField] private KeyCode nextDayKey = KeyCode.F1;
    [SerializeField] private KeyCode settlementKey = KeyCode.F8;

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
        if (spawner == null) spawner = FindObjectOfType<PrisonerSpawnController>(); // ✅ 추가

        cellManager?.BuildCellsIfNeeded();
    }

    private void Update()
    {
        if (Input.GetKeyDown(nextDayKey)) StartNextDay();
        if (Input.GetKeyDown(settlementKey)) RunSettlement();
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

        if (settlement.IsRiotOver())
        {
            Debug.LogWarning("[Ending] Bad Ending_산업 재해 (Standby Riot Over)");
            _ended = true;
            return;
        }

        // ✅ 하루 시작 전: 기존 스폰 싹 정리
        spawner?.ClearAllForNewDay();

        // Standby
        cellManager.RunStandbySetup();

        // ✅ Standby 결과로 스폰
        var activeIds = cellManager.GetActiveCellIds();
        spawner?.SpawnForToday(activeIds, id => cellManager.GetCell(id).IsSuspicious);

        // 리포트 캐시(Resolved 누적) 초기화
        reportBuilder.ClearResolvedCache();

        if (autoPickFirstActiveCell)
        {
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
            Debug.LogWarning("[Ending] Bad Ending_산업 재해 (Settlement Riot Over)");
            _ended = true;
            return;
        }

        if (day >= maxDays)
        {
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
