using System;
using System.Collections;
using UnityEngine;

public class MissionBriefingNPC : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    [SerializeField] private MissionDialogueDatabase dialogueDatabase;
    [SerializeField] private DialogueManager dialogueManager;

    private bool _busy;

    private Action<SettlementReportConfirmedEvent> _onReportConfirmed;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindAnyObjectByType<DialogueManager>();

        _onReportConfirmed = OnReportConfirmed;
        _onUIHardReset = OnUIHardReset;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onReportConfirmed); 
        EventBus.Subscribe(_onUIHardReset); 
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onReportConfirmed);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    public void Interact(Player player)
    {
        if (_busy) return;
        if (dialogueManager == null || dialogueDatabase == null) return;

        var missionManager = DailyMissionManager.Instance;
        if (missionManager == null || missionManager.CurrentMission == null) return;

        var mission = missionManager.CurrentMission;
        var dialogueData = dialogueDatabase.GetDialogueData(mission.missionId);
        if (dialogueData == null) return;

        // 1) 최초 브리핑
        if (!missionManager.IsBriefingCompleted)
        {
            _busy = true;
            dialogueManager.StartDialogue(dialogueData.briefing);
            StartCoroutine(Co_WaitBriefingEnd());
            return;
        }

        // 2) 보고 시도 (Patrol 제한)
        if (!missionManager.IsReported)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.Patrol)
            {
                // 경고 팝업
                EventBus.Publish(new ShowTimedTextPopupEvent("순찰하지 않으면 보고 할 수 없어", 1f));
                return;
            }

            EventBus.Publish(new ShowSettlementConfirmPopupEvent());
        }
    }

    private IEnumerator Co_WaitBriefingEnd()
    {
        yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);

        DailyMissionManager.Instance.MarkBriefingCompleted();
        EventBus.Publish(new MissionBriefingDialogueEndedEvent(DailyMissionManager.Instance.CurrentMission));

        _busy = false;
    }

    // Confirm 이후 호출되는 엔트리
    private void OnReportConfirmed(SettlementReportConfirmedEvent e)
    {
        if (_busy) return;
        StartCoroutine(Co_PlayResultDialogue());
    }

    private IEnumerator Co_PlayResultDialogue()
    {
        var missionManager = DailyMissionManager.Instance;
        if (missionManager == null || missionManager.CurrentMission == null)
            yield break;

        var mission = missionManager.CurrentMission;
        var data = dialogueDatabase.GetDialogueData(mission.missionId);
        if (data == null)
            yield break;

        bool success = missionManager.EvaluateDayResult(out string failReason);

        _busy = true;

        // 공통(fin)
        if (data.fin != null && data.fin.Length > 0)
        {
            dialogueManager.StartDialogue(data.fin);
            yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);
        }

        // 성공/실패
        var resultLines = success ? data.success : data.fail;
        if (resultLines != null && resultLines.Length > 0)
        {
            dialogueManager.StartDialogue(resultLines);
            yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);
        }

        // [유지/중요] 대화가 끝난 뒤에만 ResultUI 표시
        EventBus.Publish(new ResultUIShowRequestedEvent(success, failReason));

        _busy = false;
    }

    private void OnUIHardReset(UIHardResetEvent e) // [추가]
    {
        _busy = false;
    }
}





