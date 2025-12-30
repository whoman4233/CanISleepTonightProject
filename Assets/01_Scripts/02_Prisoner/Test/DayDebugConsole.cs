using System.Collections.Generic;
using UnityEngine;

public class DayDebugConsole : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PrisonManager cellManager;
    [SerializeField] private InspectionStateMachine inspection;
    [SerializeField] private SettlementReportBuilder report;
    // [삭제] private GameBootstrap bootstrap; 

    [Header("Debug Target")]
    [Tooltip("체크 시, 오늘 활성화된 첫 번째 방을 자동으로 타겟팅합니다.")]
    [SerializeField] private bool autoPickActiveCell = true; // 이름 변경
    [SerializeField] private string testCellId = "C_1F_01"; // 수동 타겟

    private void Awake()
    {
        if (cellManager == null) cellManager = FindObjectOfType<PrisonManager>();
        if (inspection == null) inspection = FindObjectOfType<InspectionStateMachine>();
        if (report == null) report = FindObjectOfType<SettlementReportBuilder>();

        // [삭제] bootstrap = FindObjectOfType<GameBootstrap>();

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

        // F9: Report Preview
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
        // 1. 자동 타겟팅이 꺼져있으면 수동 ID 반환
        if (!autoPickActiveCell) return testCellId;

        // 2. 매니저가 없으면 수동 ID 반환
        if (cellManager == null) return testCellId;

        // 3. [변경] PrisonManager에게 직접 활성 목록을 물어봄
        // (PrisonManager에 GetActiveCellIds 메서드가 있어야 함)
        var activeIds = cellManager.GetActiveCellIds();

        if (activeIds != null && activeIds.Count > 0)
        {
            return activeIds[0]; // 첫 번째 활성 방 리턴
        }

        return testCellId; // 활성 방 없으면 기본값
    }
}