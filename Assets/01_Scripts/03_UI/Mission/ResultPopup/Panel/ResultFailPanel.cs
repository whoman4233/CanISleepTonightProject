using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultFailPanel : ResultPanelBase
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI systemDayText;

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button titleButton;

    protected override void Awake()
    {
        base.Awake();

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (titleButton != null)
            titleButton.onClick.AddListener(OnTitleClicked);
    }

    private void OnDestroy()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(OnRestartClicked);

        if (titleButton != null)
            titleButton.onClick.RemoveListener(OnTitleClicked);
    }

    public override void Show()
    {
        base.Show();

        int currentDay = GameManager.Instance.CurrentDay;
        systemDayText.text = $"{currentDay}";
    }

    private void OnRestartClicked()
    {
        GameManager.Instance.SetStandbyEnterReason(StandbyEnterReason.RestartSameDay);
        EventBus.Publish(new RequestRestartFromFailureEvent());
    }

    private void OnTitleClicked()
    {
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }
}
