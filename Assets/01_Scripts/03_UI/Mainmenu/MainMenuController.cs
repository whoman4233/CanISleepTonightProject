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

    [Header("Settings Buttons")]
    [SerializeField] private Button btnSettingsBack;

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

        btnSettingsBack.onClick.AddListener(OnClickBackToMain);
    }
    public void ResetState() //MainMenuRootController 에서 초기화
    {
        mainButtonGroup.SetActive(true);
        startButtonGroup.SetActive(false);
    }

    private void OnClickStart()
    {
        mainButtonGroup.SetActive(false);
        startButtonGroup.SetActive(true);
    }

    private void OnClickSettings()
    {
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

    private void OnClickLoadGame()
    {
        EventBus.Publish(new LoadGameEvent());
    }
}

