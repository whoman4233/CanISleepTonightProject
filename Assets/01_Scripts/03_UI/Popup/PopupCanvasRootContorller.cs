using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PopupCanvasRootController : MonoBehaviour
{
    [Header("Popups")]
    [SerializeField] private ExitConfirmPopupController exitConfirmPopup;
    [SerializeField] private SettingsPopupController settingsPopup;
    [SerializeField] private WhiteBoardPopupController whiteBoardPopup;
    [SerializeField] private SettlementConfirmPopupController settlementConfirmPopup;

    private Action<ShowExitConfirmPopupEvent> _onShowExit;
    private Action<ShowSettingsPopupEvent> _onShowSettings;
    private Action<HideSettingsPopupEvent> _onHideSettings;
    private Action<ShowWhiteBoardPopupEvent> _onShowWhiteBoard;
    private Action<HideWhiteBoardPopupEvent> _onHideWhiteBoard;
    private Action<PopupCloseRequestedEvent> _onPopupCloseRequested;
    private Action<ShowSettlementConfirmPopupEvent> _onShowSettlementConfirm;

    public bool HasAnyPopupOpen =>
     (exitConfirmPopup != null && exitConfirmPopup.gameObject.activeInHierarchy) ||
     (settingsPopup != null && settingsPopup.gameObject.activeInHierarchy) ||
     (whiteBoardPopup != null && whiteBoardPopup.gameObject.activeInHierarchy);

    private void Awake()
    {
        _onShowExit = OnShowExitConfirm;
        _onShowSettings = OnShowSettings;
        _onHideSettings = OnHideSettings;
        _onShowWhiteBoard = OnShowWhiteBoard;
        _onHideWhiteBoard = OnHideWhiteBoard;
        _onPopupCloseRequested = OnPopupCloseRequested;
        _onShowSettlementConfirm = OnShowSettlementConfirm;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShowExit);
        EventBus.Subscribe(_onShowSettings);
        EventBus.Subscribe(_onHideSettings);
        EventBus.Subscribe(_onShowWhiteBoard);
        EventBus.Subscribe(_onHideWhiteBoard);
        EventBus.Subscribe(_onPopupCloseRequested);
        EventBus.Subscribe(_onShowSettlementConfirm);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShowExit);
        EventBus.Unsubscribe(_onShowSettings);
        EventBus.Unsubscribe(_onHideSettings);
        EventBus.Unsubscribe(_onShowWhiteBoard);
        EventBus.Unsubscribe(_onHideWhiteBoard);
        EventBus.Unsubscribe(_onPopupCloseRequested);
        EventBus.Unsubscribe(_onShowSettlementConfirm);
    }

    private void OnShowExitConfirm(ShowExitConfirmPopupEvent e)
    {
        if (exitConfirmPopup == null) return;
        StartCoroutine(ShowPopupStable(exitConfirmPopup.Show));
    }

    private void OnShowSettings(ShowSettingsPopupEvent e)
    {
        if (settingsPopup == null) return;
        StartCoroutine(ShowPopupStable(settingsPopup.Show));
    }

    private void OnHideSettings(HideSettingsPopupEvent e)
    {
        if (settingsPopup == null) return;
        settingsPopup.Hide();
    }

    private void OnShowWhiteBoard(ShowWhiteBoardPopupEvent e)
    {
        if (whiteBoardPopup == null) return;
        StartCoroutine(ShowPopupStable(whiteBoardPopup.Show));
    }

    private void OnHideWhiteBoard(HideWhiteBoardPopupEvent e)
    {
        if (whiteBoardPopup == null) return;
        whiteBoardPopup.Hide();
    }
    private void OnShowSettlementConfirm(ShowSettlementConfirmPopupEvent e)
    {
        if (settlementConfirmPopup == null) return;
        StartCoroutine(ShowPopupStable(settlementConfirmPopup.Show));
    }

    private IEnumerator ShowPopupStable(Action showAction)
    {
        yield return null;

        showAction?.Invoke();

        yield return null;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void OnPopupCloseRequested(PopupCloseRequestedEvent e)
    {
        if (!HasAnyPopupOpen)
            return;

        if (whiteBoardPopup != null && whiteBoardPopup.gameObject.activeInHierarchy)
        {
            whiteBoardPopup.Hide();
            return;
        }

        if (settingsPopup != null && settingsPopup.gameObject.activeInHierarchy)
        {
            settingsPopup.Hide();
            return;
        }

        if (exitConfirmPopup != null && exitConfirmPopup.gameObject.activeInHierarchy)
        {
            exitConfirmPopup.Hide();
            return;
        }
    }

    public void CloseAllPopups()
    {
        if (exitConfirmPopup != null) exitConfirmPopup.Hide();
        if (settingsPopup != null) settingsPopup.Hide();
        if (whiteBoardPopup != null) whiteBoardPopup.Hide();
    }
}






