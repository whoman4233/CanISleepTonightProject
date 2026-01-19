using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SeniorGuard : MonoBehaviour, IInteractable
{
    [Header("Dialogue Settings")]
    [SerializeField] private string speakerKey = DialogueKeys.Speakers.Frank;
    //[SerializeField] private string missionKey = DialogueKeys.Missions.Mission06;

    public void Interact(Player player)
    {
        if (DailyMissionManager.Instance.CurrentMission is Mission06Strategy m06)
        {
            if (m06.HasReported)
            {
                // 선택지 1: 완전 무반응
                return;

                // 선택지 2: 경고 텍스트 (원하면)
                // EventBus.Publish(new ShowTimedTextPopupEvent("이미 보고를 마쳤다.", 1.5f));
            }
            System.Action onDialogueEnd = () =>
            {
                EventBus.Publish(new Mission06PuzzleShowRequestedEvent());
            };

            DialogueManager.Instance.StartDialogueByKeys(
                speakerKey,
                DialogueKeys.Types.Fin,
                onDialogueEnd
            );
        }
        else
        {
            DialogueManager.Instance.StartDialogueByKeys(speakerKey, DialogueKeys.Types.Dialogue);
        }
    }
}
