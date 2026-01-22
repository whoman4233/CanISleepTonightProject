using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PuzzleController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject puzzleRoot;

    [Header("Buttons")]
    [SerializeField] private Button antonyButton;
    [SerializeField] private Button richardButton;
    [SerializeField] private Button leoButton;
    [SerializeField] private Button backButton;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI antonyText;
    [SerializeField] private TextMeshProUGUI richardText;
    [SerializeField] private TextMeshProUGUI leoText;
    //[SerializeField] private TextMeshProUGUI back;

    private Action<Mission06PuzzleShowRequestedEvent> _onShow;

    private void Awake()
    {
        _onShow = OnShowRequested;

        antonyButton.onClick.AddListener(() => OnSuspectClicked(0));
        richardButton.onClick.AddListener(() => OnSuspectClicked(1));
        leoButton.onClick.AddListener(() => OnSuspectClicked(2));
        backButton.onClick.AddListener(() => OnBackClick());

        puzzleRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
    }

    private void OnShowRequested(Mission06PuzzleShowRequestedEvent e)
    {
        var mission = DailyMissionManager.Instance?.CurrentMission as Mission06Strategy;
        if (mission == null)
            return;

        // 이름 세팅 (Mission06Data 기반)
        antonyText.text = mission.MissionData.Suspect1Name;
        richardText.text = mission.MissionData.Suspect2Name;
        leoText.text = mission.MissionData.Suspect3Name;

        puzzleRoot.SetActive(true);

        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());

        InputManager.Instance?.SetDialogueActive(true);
    }

    private void OnSuspectClicked(int index)
    {
        puzzleRoot.SetActive(false);

        EventBus.Publish(new Mission06SuspectSelectedEvent
        {
            selectedIndex = index
        });

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
        DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, DialogueKeys.DialogueType.AfterChoice.ToString()); // 미션6 선택지 선택 후 나오는 대사 강제 출력

        InputManager.Instance?.SetDialogueActive(false);
    }

    private void OnBackClick()
    {
        puzzleRoot.SetActive(false);
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
        InputManager.Instance?.SetDialogueActive(false);
    }
}
