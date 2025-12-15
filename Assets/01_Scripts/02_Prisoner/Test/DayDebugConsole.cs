using System.Collections.Generic;
using UnityEngine;

public class DayDebugConsole : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementReportBuilder report;

    [Header("Debug Target")]
    [SerializeField] private string testCellId = "C_1F_01";

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (report == null) report = FindObjectOfType<SettlementReportBuilder>();

        if (cellManager != null)
        {
            cellManager.OnNoiseChanged += (id, noisy) =>
            {
                Debug.Log($"[NoiseChanged] {id} noisy={noisy}");
            };
        }

        if (inspection != null)
        {
            inspection.OnEnteredCell += id => Debug.Log($"[Enter] {id}");
            inspection.OnExitBlocked += id => Debug.LogWarning($"[ExitBlocked] {id} (Suppressing, not success yet)");
            inspection.OnSuppressStarted += id => Debug.Log($"[SuppressStart] {id} (Door/Exit LOCK)");
            inspection.OnSuppressSuccess += id => Debug.Log($"[SuppressSuccess] {id}");
            inspection.OnResolved += (id, susp, didSup) =>
                Debug.Log($"[Resolved] {id} susp={susp} didSuppress={didSup}");
        }
    }

    private void Update()
    {
        // F1: Standby 셋업
        if (Input.GetKeyDown(KeyCode.F1))
        {
            cellManager.RunStandbySetup();
            DumpToday();
        }

        // F2: Enter
        if (Input.GetKeyDown(KeyCode.F2))
        {
            bool ok = inspection.TryEnterCell(testCellId);
            Debug.Log($"[TryEnter] {testCellId} => {ok}");
        }

        // F3: Warning(NonSuppress)
        if (Input.GetKeyDown(KeyCode.F3))
        {
            bool ok = inspection.SelectWarning(testCellId);
            Debug.Log($"[SelectWarning] {testCellId} => {ok}");
        }

        // F4: Suppress start (Lock)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            bool ok = inspection.SelectSuppress(testCellId);
            Debug.Log($"[SelectSuppress] {testCellId} => {ok}");
        }

        // F5: Suppress success signal
        if (Input.GetKeyDown(KeyCode.F5))
        {
            bool ok = inspection.NotifySuppressSuccess(testCellId);
            Debug.Log($"[NotifySuppressSuccess] {testCellId} => {ok}");
        }

        // F6: Exit request
        if (Input.GetKeyDown(KeyCode.F6))
        {
            bool ok = inspection.RequestExitCell(testCellId);
            Debug.Log($"[RequestExit] {testCellId} => {ok}");
        }

        // F7: Time Expired (Force release, no resolve)
        if (Input.GetKeyDown(KeyCode.F7))
        {
            inspection.ForceReleaseOnTimeExpired();
            Debug.Log("[TimeExpired] ForceReleaseOnTimeExpired()");
        }

        // F8: Settlement Report build (1회 패킷)
        if (Input.GetKeyDown(KeyCode.F8))
        {
            report.BuildSettlementReport(out List<ResolvedRecord> resolved, out List<UninspectedRecord> uninspected);

            Debug.Log($"[SettlementReport] Resolved={resolved.Count}, Uninspected={uninspected.Count}");
            foreach (var r in resolved)
                Debug.Log($"  - RESOLVED: {r.cellId} susp={r.isSuspicious} didSuppress={r.didSuppress}");
            foreach (var u in uninspected)
                Debug.Log($"  - UNINSPECTED: {u.cellId} susp={u.isSuspicious}");
        }
    }

    private void DumpToday()
    {
        Debug.Log("=== Today Active Cells ===");
        foreach (var id in cellManager.GetActiveCellIds())
        {
            var c = cellManager.GetCell(id);
            Debug.Log($"  - {c}");
        }
    }
}
