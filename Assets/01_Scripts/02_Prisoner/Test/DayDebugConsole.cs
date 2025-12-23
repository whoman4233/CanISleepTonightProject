using System.Collections.Generic;
using UnityEngine;

public class DayDebugConsole : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonCellManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementReportBuilder report;
    [SerializeField] private GameBootstrap bootstrap; // ✅ 추가(자동 대상)

    [Header("Debug Target")]
    [SerializeField] private bool followBootstrapTarget = true;
    [SerializeField] private string testCellId = "C_1F_01";

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonCellManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (report == null) report = FindObjectOfType<SettlementReportBuilder>();
        if (bootstrap == null) bootstrap = FindObjectOfType<GameBootstrap>();

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
        var target = ResolveTargetCellId();
        if (string.IsNullOrWhiteSpace(target)) return;

        // F2: Enter
        if (Input.GetKeyDown(KeyCode.F2))
        {
            bool ok = inspection.TryEnterCell(target);
            Debug.Log($"[TryEnter] {target} => {ok}");
        }


        // F4: Suppress start (Lock)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            bool ok = inspection.SelectSuppress(target);
            Debug.Log($"[SelectSuppress] {target} => {ok}");
        }

        // F5: Suppress success signal
        if (Input.GetKeyDown(KeyCode.F5))
        {
            bool ok = inspection.NotifySuppressSuccess(target);
            Debug.Log($"[NotifySuppressSuccess] {target} => {ok}");
        }

        // F6: Exit request
        if (Input.GetKeyDown(KeyCode.F6))
        {
            bool ok = inspection.RequestExitCell(target);
            Debug.Log($"[RequestExit] {target} => {ok}");
        }

        // F7: Time Expired (Force release, no resolve)
        if (Input.GetKeyDown(KeyCode.F7))
        {
            inspection.ForceReleaseOnTimeExpired();
            Debug.Log("[TimeExpired] ForceReleaseOnTimeExpired()");
        }

        // (선택) 리포트만 확인하고 싶으면 F9 같은 남는 키로
        if (Input.GetKeyDown(KeyCode.F9))
        {
            report.BuildSettlementReport(out List<ResolvedRecord> resolved, out List<UninspectedRecord> uninspected);

            Debug.Log($"[SettlementReport-Preview] Resolved={resolved.Count}, Uninspected={uninspected.Count}");
            foreach (var r in resolved)
                Debug.Log($"  - RESOLVED: {r.cellId} susp={r.isSuspicious} didSuppress={r.didSuppress}");
            foreach (var u in uninspected)
                Debug.Log($"  - UNINSPECTED: {u.cellId} susp={u.isSuspicious}");
        }
    }

    private string ResolveTargetCellId()
    {
        if (!followBootstrapTarget) return testCellId;
        if (bootstrap == null) return testCellId;

        var id = bootstrap.CurrentTestCellId;
        return string.IsNullOrWhiteSpace(id) ? testCellId : id;
    }
}
