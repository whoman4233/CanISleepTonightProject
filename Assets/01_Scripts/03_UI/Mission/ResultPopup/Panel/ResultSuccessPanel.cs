using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultSuccessPanel : ResultPanelBase
{
    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI systemDayText;

    [Header("Buttons")]
    [SerializeField] private Button nextDayButton;
    [SerializeField] private Button titleButton;

    protected override void Awake()
    {
        base.Awake();

        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(OnNextDayClicked);

        if (titleButton != null)
            titleButton.onClick.AddListener(OnTitleClicked);
    }

    private void OnDestroy()
    {
        if (nextDayButton != null)
            nextDayButton.onClick.RemoveListener(OnNextDayClicked);

        if (titleButton != null)
            titleButton.onClick.RemoveListener(OnTitleClicked);
    }

    public override void Show()
    {
        base.Show();

        int currentDay = GameManager.Instance.CurrentDay;
        systemDayText.text = $"{currentDay} -> {currentDay + 1}";
    }

    private void OnNextDayClicked()
    {
        DailyMissionManager.Instance.MarkReported();
        EventBus.Publish(new RequestSceneReloadEvent());
    }

    private void OnTitleClicked()
    {
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }
}
