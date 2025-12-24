using System;
using UnityEngine;

public class PopupCanvasRootController : MonoBehaviour
{
    [SerializeField] private ExitConfirmPopupController exitConfirmPopup;
    [SerializeField] private SettingsPopupController settingsPopup;
    [SerializeField] private WhiteBoardPopupController whiteBoardPopup;

    private Action<ShowExitConfirmPopupEvent> showExitHandler;
    private Action<ShowSettingsPopupEvent> showSettingsHandler;
    private Action<HideSettingsPopupEvent> hideSettingsHandler;
    private Action<ShowWhiteBoardPopupEvent> showWhiteBoardHandler;
    private Action<HideWhiteBoardPopupEvent> hideWhiteBoardHandler;
    private Action<PopupCloseRequestedEvent> closePopupHandler;

    public bool HasAnyPopupOpen =>
    whiteBoardPopup.IsOpen ||
    settingsPopup.IsOpen ||
    exitConfirmPopup.IsOpen;

    private void Awake()
    {
        showExitHandler = OnShowExitConfirm;
        showSettingsHandler = OnShowSettings;
        hideSettingsHandler = OnHideSettings;
        showWhiteBoardHandler = OnShowWhiteBoard;
        hideWhiteBoardHandler = OnHideWhiteBoard;
        closePopupHandler = OnPopupCloseRequested;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(showExitHandler);
        EventBus.Subscribe(showSettingsHandler);
        EventBus.Subscribe(hideSettingsHandler);
        EventBus.Subscribe(showWhiteBoardHandler);
        EventBus.Subscribe(hideWhiteBoardHandler);
        EventBus.Subscribe(closePopupHandler);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(showExitHandler);
        EventBus.Unsubscribe(showSettingsHandler);
        EventBus.Unsubscribe(hideSettingsHandler);
        EventBus.Unsubscribe(showWhiteBoardHandler);
        EventBus.Unsubscribe(hideWhiteBoardHandler);
        EventBus.Unsubscribe(closePopupHandler);
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

    private void OnShowWhiteBoard(ShowWhiteBoardPopupEvent e)
    {
        whiteBoardPopup.Show();
    }

    private void OnHideWhiteBoard(HideWhiteBoardPopupEvent e)
    {
        whiteBoardPopup.Hide();
    }

    public void CloseAllPopups()
    {
        exitConfirmPopup.Hide();
        settingsPopup.Hide();
        whiteBoardPopup.Hide();
    }
    private void OnPopupCloseRequested(PopupCloseRequestedEvent e)
    {
        // 우선순위: WhiteBoard > Settings > ExitConfirm
        if (whiteBoardPopup.IsOpen)
        {
            whiteBoardPopup.Hide();
            return;
        }

        if (settingsPopup.IsOpen)
        {
            settingsPopup.Hide();
            return;
        }

        if (exitConfirmPopup.IsOpen)
        {
            exitConfirmPopup.Hide();
            return;
        }
    }
}



