using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class MainMenuRootController : MonoBehaviour
{
    [Header("Menu Root")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private MainMenuController menuController;

    private Action<RequestStartNewGameEvent> _onRequestStartNewGame;
    private Action<ShowSettingsPopupEvent> _onShowSettings;
    private Action<HideSettingsPopupEvent> _onHideSettings;

    // =========================
    //  Input
    // =========================
    private PlayerInputs _inputs;
    private bool _settingsOpen;

    private void Awake()
    {
        _onRequestStartNewGame = OnRequestStartNewGame;
        _onShowSettings = OnShowSettings;
        _onHideSettings = OnHideSettings;

        _inputs = InputManager.Instance.Inputs;

        if (menuRoot != null)
            menuRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onRequestStartNewGame);
        EventBus.Subscribe(_onShowSettings);
        EventBus.Subscribe(_onHideSettings);

        Show(); // 앱 최초 진입 시 메인메뉴 표시
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onRequestStartNewGame);
        EventBus.Unsubscribe(_onShowSettings);
        EventBus.Unsubscribe(_onHideSettings);
    }

    // =========================
    // Event Handlers
    // =========================

    private void OnRequestStartNewGame(RequestStartNewGameEvent e)
    {
        Debug.Log("[MainMenu] RequestStartNewGameEvent → Hide");
        Hide();
    }

    private void OnShowSettings(ShowSettingsPopupEvent e)
    {
        _settingsOpen = true; // [ADDED]

        // Popup이 열리는 동안 메인메뉴 Raycast 차단
        if (menuCanvasGroup != null)
            menuCanvasGroup.blocksRaycasts = false;
    }

    private void OnHideSettings(HideSettingsPopupEvent e)
    {
        _settingsOpen = false; //

        // Popup 닫히면 Raycast 복구
        if (menuCanvasGroup != null)
            menuCanvasGroup.blocksRaycasts = true;
    }

    // =========================
    // Visibility
    // =========================

    private void Show()
    {
        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (menuCanvasGroup != null)
            menuCanvasGroup.blocksRaycasts = true;

        menuController?.ResetState();
    }

    private void Hide()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
    }
}



