using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Button Groups")]
    [SerializeField] private GameObject mainButtonGroup; //메인메뉴 버튼
    [SerializeField] private GameObject startButtonGroup; // 시작버튼 뒤에 나오는 버튼

    [Header("Main Buttons")]
    [SerializeField] private Button btnStart; // 시작버튼
    [SerializeField] private Button btnExit; // 종료 버튼
    [SerializeField] private Button btnSettings; //세팅(옵션) 버튼

    [Header("Start Buttons")]
    [SerializeField] private Button btnNewGame; // 새 게임 버튼
    [SerializeField] private Button btnLoadGame; // 이어하기 버튼
    [SerializeField] private Button btnStartBack; // 메인메뉴로 돌아가기

    [Header("Settings Buttons")]
    [SerializeField] private Button btnSettingsBack; // 세팅 뒤로가기

    private void Awake()
    {
        BindButtons();
        ResetState();
    }

    private void BindButtons()
    {
        // Main
        btnStart.onClick.AddListener(OnClickStart);
        btnExit.onClick.AddListener(OnClickExit);
        btnSettings.onClick.AddListener(OnClickSettings);

        // Start
        btnNewGame.onClick.AddListener(OnClickNewGame);
        btnLoadGame.onClick.AddListener(OnClickLoadGame);
        btnStartBack.onClick.AddListener(OnClickBackToMain);

        // Settings
        btnSettingsBack.onClick.AddListener(OnClickBackToMain);
    }

    private void ResetState()
    {
        mainButtonGroup.SetActive(true);
        startButtonGroup.SetActive(false);

    }

    // ---------- Button Callbacks ----------

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
        Debug.Log("Exit button clicked");
        EventBus.Publish(new ShowExitConfirmPopupEvent());
    }

    private void OnClickNewGame()
    {
        EventBus.Publish(new StartNewGameEvent());
    }

    private void OnClickLoadGame()
    {
        EventBus.Publish(new LoadGameEvent());
    }
}
