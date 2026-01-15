using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeniorGuard : MonoBehaviour , IInteractable
{
    [Header("Dialogue Settings")]
    [SerializeField] private string speakerKey = DialogueKeys.Speakers.Frank;
    [SerializeField] private string missionKey = DialogueKeys.Missions.Mission06;

    public void Interact(Player player)
    {
        System.Action onDialogueEnd = () =>
        {
            string[] names = { "{Suspect1}", "{Suspect2}", "{Suspect3}" };

            ChoiceButton.Instance.Open(names, (index) =>
            {
                if (DailyMissionManager.Instance.CurrentMission is Mission06Strategy m06)
                {
                    m06.SubmitReport(index);
                    Debug.Log($"[Mission06] 플레이어가 {index + 1}번 용의자를 지목함.");
                }
            });
        };
        // 대화만 먼저 실행 (선임교도관대화)
        DialogueManager.Instance.StartDialogueByKeys(speakerKey, DialogueKeys.Types.Fin, onDialogueEnd);

        // 선택지 창 열기
        string[] names = { "{Suspect1}", "{Suspect2}", "{Suspect3}" };
    }

}
