using System;
using System.Collections;
using UnityEngine;

public class MissionFailController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform missionNpcArrivalPoint;
    [SerializeField] private VignetteFadeController vignetteFade;

    private Player _player;
    private Action<SettlementReportConfirmedEvent> _onSettlementConfirmed;
    private Action<PlayerSpawnedEvent> _onPlayerSpawned;

    private DialogueManager Dialogue => DialogueManager.Instance;

    private void Awake()
    {
        _onSettlementConfirmed = OnSettlementConfirmed;
        _onPlayerSpawned = e => _player = e.Player;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onSettlementConfirmed);
        EventBus.Subscribe(_onPlayerSpawned);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onSettlementConfirmed);
        EventBus.Unsubscribe(_onPlayerSpawned);
    }

    private void OnSettlementConfirmed(SettlementReportConfirmedEvent e)
    {
        var mission = DailyMissionManager.Instance?.CurrentMission
            as Mission_FindImposterStrategy;

        if (mission == null || !mission.ImmediateFailTriggered)
            return;

        StartCoroutine(Co_ImmediateFailFlow(mission));
    }

    private IEnumerator Co_ImmediateFailFlow(Mission_FindImposterStrategy mission)
    {
        if (Dialogue == null || _player == null)
        {
            Debug.LogError("[MissionFailController] Required references missing.");
            yield break;
        }

        yield return new WaitUntil(() => !Dialogue.IsDialogueOpen);

        if (vignetteFade != null)
            yield return vignetteFade.FadeOut(this);

        MovePlayerSafely();

        if (vignetteFade != null)
            yield return vignetteFade.FadeIn(this);

        Dialogue.StartDialogueByKey(mission.ImmediateFailDialogueKey);
    }

    private void MovePlayerSafely()
    {
        if (missionNpcArrivalPoint == null)
            return;

        const float offset = 0.5f;
        Vector3 pos =
            missionNpcArrivalPoint.position -
            missionNpcArrivalPoint.forward * offset;

        _player.transform.SetPositionAndRotation(
            pos,
            missionNpcArrivalPoint.rotation
        );
    }
}





