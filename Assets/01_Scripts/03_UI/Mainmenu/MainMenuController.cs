using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Button Groups")]
    [SerializeField] private GameObject mainButtonGroup;
    [SerializeField] private GameObject startButtonGroup;

    [Header("Main Buttons")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnExit;
    [SerializeField] private Button btnSettings;

    [Header("Start Buttons")]
    [SerializeField] private Button btnNewGame;
    [SerializeField] private Button btnLoadGame;
    [SerializeField] private Button btnStartBack;

    private void Awake()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        btnStart.onClick.AddListener(OnClickStart);
        btnExit.onClick.AddListener(OnClickExit);
        btnSettings.onClick.AddListener(OnClickSettings);

        btnNewGame.onClick.AddListener(OnClickNewGame);
        btnLoadGame.onClick.AddListener(OnClickLoadGame);
        btnStartBack.onClick.AddListener(OnClickBackToMain);
    }

    /// <summary>
    /// MainMenuRootController에서 호출
    /// </summary>
    public void ResetState()
    {
        mainButtonGroup.SetActive(true);
        startButtonGroup.SetActive(false);
    }

    // =========================
    // Button Handlers
    // =========================

    private void OnClickStart()
    {
        mainButtonGroup.SetActive(false);
        startButtonGroup.SetActive(true);
    }
    private void OnClickLoadGame()
    {
        EventBus.Publish(new LoadGameEvent());
    }
    private void OnClickSettings()
    {
        // Settings는 전역 Popup
        EventBus.Publish(new ShowSettingsPopupEvent());
    }

    private void OnClickBackToMain()
    {
        ResetState();
    }

    private void OnClickExit()
    {
        EventBus.Publish(new ShowExitConfirmPopupEvent());
    }

    private void OnClickNewGame()
    {
        EventBus.Publish(new RequestStartNewGameEvent());
    }
}


