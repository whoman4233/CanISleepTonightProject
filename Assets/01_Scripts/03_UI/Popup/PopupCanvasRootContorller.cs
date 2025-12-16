using UnityEngine;

public class PopupCanvasRootController : MonoBehaviour
{
    [SerializeField] private ExitConfirmPopupController exitConfirmPopup;
    [SerializeField] private SettingsPopupController settingsPopup;

    private System.Action<ShowExitConfirmPopupEvent> showExitHandler;
    private System.Action<ShowSettingsPopupEvent> showSettingsHandler;
    private System.Action<HideSettingsPopupEvent> hideSettingsHandler;

    private void Awake()
    {
        showExitHandler = OnShowExitConfirm;
        showSettingsHandler = OnShowSettings;
        hideSettingsHandler = OnHideSettings;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(showExitHandler);
        EventBus.Subscribe(showSettingsHandler);
        EventBus.Subscribe(hideSettingsHandler);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(showExitHandler);
        EventBus.Unsubscribe(showSettingsHandler);
        EventBus.Unsubscribe(hideSettingsHandler);
    }

    private void OnShowExitConfirm(ShowExitConfirmPopupEvent e)
    {
        exitConfirmPopup.Show();
    }

    private void OnShowSettings(ShowSettingsPopupEvent e)
    {
        settingsPopup.Show();
    }

    private void OnHideSettings(HideSettingsPopupEvent e)
    {
        settingsPopup.Hide();
    }

    public void CloseAllPopups()
    {
        exitConfirmPopup.Hide();
        settingsPopup.Hide();
    }
}


