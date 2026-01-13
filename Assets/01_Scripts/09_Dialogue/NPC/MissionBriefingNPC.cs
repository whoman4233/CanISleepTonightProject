using System.Collections;
using UnityEngine;

public class MissionBriefingNPC : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    [SerializeField] private MissionDialogueDatabase dialogueDatabase;
    [SerializeField] private DialogueManager dialogueManager;

    private bool _busy;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindAnyObjectByType<DialogueManager>();
    }

    public void Interact(Player player)
    {
        if (_busy) return;
        if (dialogueManager == null || dialogueDatabase == null) return;

        var missionManager = DailyMissionManager.Instance;
        if (missionManager == null || missionManager.CurrentMission == null) return;

        var mission = missionManager.CurrentMission;
        DialogueData dialogueData =
            dialogueDatabase.GetDialogueData(mission.missionId);

        if (dialogueData == null)
        {
            Debug.LogWarning($"[MissionBriefingNPC] No DialogueData for {mission.missionId}");
            return;
        }

        bool isSettlement =
            GameManager.Instance != null &&
            GameManager.Instance.CurrentPhase == GamePhase.Settlement;

        if (!isSettlement)
        {
            // =========================
            // 브리핑
            // =========================
            if (dialogueData.briefing == null || dialogueData.briefing.Length == 0)
                return;

            _busy = true;
            dialogueManager.StartDialogue(dialogueData.briefing);
            StartCoroutine(Co_WaitBriefingEnd(mission));
        }
        else
        {
            // =========================
            // 결과
            // =========================
            bool success = missionManager.EvaluateDayResult(out string failReason);

            _busy = true;
            StartCoroutine(Co_PlayResultDialogue(
                dialogueData,
                success,
                failReason
            ));
        }
    }

    // ------------------------------------------------------

    private IEnumerator Co_WaitBriefingEnd(DailyMissionStrategy mission)
    {
        yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);

        // 브리핑 종료 → 팝업 요청
        EventBus.Publish(new MissionBriefingDialogueEndedEvent(mission));
        _busy = false;
    }

    // ------------------------------------------------------

    private IEnumerator Co_PlayResultDialogue(
        DialogueData data,
        bool success,
        string failReason
    )
    {
        // 결과 공통 대사
        if (data.fin != null && data.fin.Length > 0)
        {
            dialogueManager.StartDialogue(data.fin);
            yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);
        }

        // 성공 / 실패 대사
        DialogueLine[] resultLines = success ? data.success : data.fail;

        if (resultLines != null && resultLines.Length > 0)
        {
            dialogueManager.StartDialogue(resultLines);
            yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);
        }

        // 결과 종료 이벤트
        EventBus.Publish(new MissionReportDialogueEndedEvent(success, failReason));
        _busy = false;
    }
}
