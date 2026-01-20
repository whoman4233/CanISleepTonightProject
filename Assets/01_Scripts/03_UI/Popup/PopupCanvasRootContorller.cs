using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PopupCanvasRootController : MonoBehaviour
{
    [Header("Popups")]
    [SerializeField] private ExitConfirmPopupController exitConfirmPopup;
    [SerializeField] private SettingsPopupController settingsPopup;
    [SerializeField] private SettlementConfirmPopupController settlementConfirmPopup;
    [SerializeField] private GameOverPopupController gameOverPopup;

    private Action<ShowExitConfirmPopupEvent> _onShowExit;
    private Action<ShowSettingsPopupEvent> _onShowSettings;
    private Action<HideSettingsPopupEvent> _onHideSettings;
    private Action<PopupCloseRequestedEvent> _onPopupCloseRequested;
    private Action<ShowSettlementConfirmPopupEvent> _onShowSettlementConfirm;
    private Action<GameOverEvent> _onGameOver;
    public bool HasAnyPopupOpen =>
   (exitConfirmPopup != null && exitConfirmPopup.gameObject.activeInHierarchy) ||
   (settingsPopup != null && settingsPopup.gameObject.activeInHierarchy) ||
   (gameOverPopup != null && gameOverPopup.gameObject.activeInHierarchy);

    private void Awake()
    {
        _onShowExit = OnShowExitConfirm;
        _onShowSettings = OnShowSettings;
        _onHideSettings = OnHideSettings;
        _onPopupCloseRequested = OnPopupCloseRequested;
        _onShowSettlementConfirm = OnShowSettlementConfirm;
        _onGameOver = OnGameOver;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShowExit);
        EventBus.Subscribe(_onShowSettings);
        EventBus.Subscribe(_onHideSettings);
        EventBus.Subscribe(_onPopupCloseRequested);
        EventBus.Subscribe(_onShowSettlementConfirm);
        EventBus.Subscribe(_onGameOver);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShowExit);
        EventBus.Unsubscribe(_onShowSettings);
        EventBus.Unsubscribe(_onHideSettings);
        EventBus.Unsubscribe(_onPopupCloseRequested);
        EventBus.Unsubscribe(_onShowSettlementConfirm);
        EventBus.Unsubscribe(_onGameOver);
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
    private void OnGameOver(GameOverEvent e)
    {
        // 다른 팝업 전부 닫기
        CloseAllPopups();

        // 게임오버 팝업 표시
        if (gameOverPopup != null)
            gameOverPopup.Show();
    }
    public void CloseAllPopups()
    {
        if (exitConfirmPopup != null) exitConfirmPopup.Hide();

        if (settingsPopup != null) settingsPopup.Hide();

        if (gameOverPopup != null) gameOverPopup.Hide();
    }
}






