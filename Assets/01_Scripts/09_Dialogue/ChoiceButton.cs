using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceButton : MonoBehaviour
{
    public static ChoiceButton Instance { get; private set; }

    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TextMeshProUGUI[] choiceTexts;

    private void Awake() => Instance = this;

    public void Open(string[] choices, Action<int> onSelected)
    {
        rootPanel.SetActive(true);

        // 커서 해제
        EventBus.Publish(new CursorOverrideRequestedEvent { HideCursor = false, LockMode = CursorLockMode.None });

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);

                // 미션 전략을 통해 랜덤 배치 된 이름 치환
                string processedName = DailyMissionManager.Instance.CurrentMission.GetProcessedText(choices[i]);
                choiceTexts[i].text = processedName;

                int index = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => {
                    rootPanel.SetActive(false);
                    EventBus.Publish(new CursorOverrideReleasedEvent());
                    onSelected?.Invoke(index);
                });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }
}
