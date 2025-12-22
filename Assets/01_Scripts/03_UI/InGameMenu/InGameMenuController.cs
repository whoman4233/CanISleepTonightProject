using UnityEngine;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnReturnToTitle;
    [SerializeField] private Button btnOptions;

    private bool isOpen;

    private void Awake()
    {
        menuRoot.SetActive(false);
        btnResume.onClick.AddListener(OnClickResume);
        btnReturnToTitle.onClick.AddListener(OnClickReturnToTitle);
        btnOptions.onClick.AddListener(OnClickOptions);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PauseMenuToggleRequestedEvent>(OnToggleRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PauseMenuToggleRequestedEvent>(OnToggleRequested);
    }

    private void OnToggleRequested(PauseMenuToggleRequestedEvent e)
    {
        SetOpen(!isOpen);
    }

    private void OnClickResume()
    {
        SetOpen(false);
    }

    private void OnClickReturnToTitle()
    {
        SetOpen(false);
        EventBus.Publish(new ResumeGameRequestedEvent());
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }

    private void OnClickOptions()
    {
        EventBus.Publish(new ShowSettingsPopupEvent());
    }

    private void SetOpen(bool open)
    {
        if (isOpen == open)
            return;

        isOpen = open;
        menuRoot.SetActive(open);

        if (open)
        {
            EventBus.Publish(new PauseGameRequestedEvent());
            EventBus.Publish(new InputModeChangedEvent(InputMode.UIOnly));
        }
        else
        {
            EventBus.Publish(new ResumeGameRequestedEvent());
            EventBus.Publish(new InputModeChangedEvent(InputMode.Gameplay));
        }
    }
}



