using System;
using System.Collections;
using UnityEngine;

public class MissionFailController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform missionNpcPoint;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Player player;
    [SerializeField] private VignetteFadeController vignetteFade;

    private Action<SettlementReportConfirmedEvent> _onSettlementConfirmed;

    private void Awake()
    {
        _onSettlementConfirmed = OnSettlementConfirmed;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onSettlementConfirmed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onSettlementConfirmed);
    }

    private void OnSettlementConfirmed(SettlementReportConfirmedEvent e)
    {
        var missionManager = DailyMissionManager.Instance;
        if (missionManager == null || missionManager.CurrentMission == null)
            return;

        bool success = missionManager.EvaluateDayResult(out _);
        if (success)
            return;

        StartCoroutine(Co_FailFlow());
    }

    private IEnumerator Co_FailFlow()
    {
        // 1. 실패 결과 다이얼로그 종료 대기
        yield return new WaitUntil(() => !dialogueManager.IsDialogueOpen);

        // 2. Vignette Fade Out
        if (vignetteFade != null)
            yield return vignetteFade.FadeOut(this);

        // 3. 강제 이동
        if (player != null && missionNpcPoint != null)
        {
            player.transform.position = missionNpcPoint.position;
            player.transform.rotation = missionNpcPoint.rotation;
        }

        // 4. Fade In
        if (vignetteFade != null)
            yield return vignetteFade.FadeIn(this);

        // 5. 실패 연계 다이얼로그
        dialogueManager.StartDialogueByKeys(
            DialogueKeys.Speakers.Frank,
            DialogueKeys.Types.Fail
        );
    }
}


