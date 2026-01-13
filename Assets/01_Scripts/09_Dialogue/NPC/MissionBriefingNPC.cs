using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionBriefingNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogue Sets (Theme-based SO)")]
    [SerializeField] private List<MissionDialogueSet> dialogueSets = new();

    [Header("Refs")]
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
        if (dialogueManager == null) return;

        var missionManager = DailyMissionManager.Instance;
        if (missionManager == null || missionManager.CurrentMission == null) return;

        // 1) 오늘 미션의 테마
        MissionDayTheme todayTheme = missionManager.CurrentMission.missionTheme;

        // 2) 테마에 맞는 DialogueSet 찾기
        MissionDialogueSet set = FindSet(todayTheme);
        if (set == null)
        {
            Debug.LogWarning(
                $"[MissionBriefingNPC] No MissionDialogueSet for theme: {todayTheme}",
                this
            );
            return;
        }

        // 3) 브리핑 / 결과 분기 (GamePhase 기준)
        bool isSettlementPhase =
            GameManager.Instance != null &&
            GameManager.Instance.CurrentPhase == GamePhase.Settlement;

        if (!isSettlementPhase)
        {
            // 브리핑
            if (set.briefing == null) return;

            _busy = true;
            dialogueManager.StartDialogue(set.briefing);
            StartCoroutine(Co_WaitDialogueEnd_Briefing(missionManager.CurrentMission));
        }
        else
        {
            // 결과 (성공 / 실패)
            bool success = missionManager.EvaluateDayResult(out string failReason);

            DialogueData reportData = success ? set.reportSuccess : set.reportFail;
            if (reportData == null) return;

            _busy = true;
            dialogueManager.StartDialogue(reportData);
            StartCoroutine(Co_WaitDialogueEnd_Report(success, failReason));
        }
    }

    private MissionDialogueSet FindSet(MissionDayTheme todayTheme)
    {
        foreach (var set in dialogueSets)
        {
            if (set == null) continue;

            // Flags 포함 관계 매칭
            if ((todayTheme & set.theme) != 0)
                return set;
        }
        return null;
    }

    private IEnumerator Co_WaitDialogueEnd_Briefing(DailyMissionStrategy mission)
    {
        yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);

        EventBus.Publish(new MissionBriefingDialogueEndedEvent(mission));
        _busy = false;
    }

    private IEnumerator Co_WaitDialogueEnd_Report(bool success, string failReason)
    {
        yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);

        EventBus.Publish(new MissionReportDialogueEndedEvent(success, failReason));
        _busy = false;
    }
}

