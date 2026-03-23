using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct GuideTextMapping
{
    public string localizationKey; // CSV에 적은 키
    public TextMeshProUGUI tmpComponent; // 연결할 TMP
}

public class ControlGuide : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject rootPanel; // 최상단 How To Play 오브젝트

    [Header("Page Management")]
    [SerializeField] private List<GameObject> pages;
    private int _currentIndex = 0;

    [Header("Navigation")]
    [SerializeField] private Button nextBtn;
    [SerializeField] private Button prevBtn;

    [Header("Localization Guide Texts")]
    [SerializeField] private TextMeshProUGUI moveText;    // Ttxt_G_01
    [SerializeField] private TextMeshProUGUI dashText;    // Ttxt_G_02
    [SerializeField] private TextMeshProUGUI sitText;  // Ttxt_G_03
    [SerializeField] private TextMeshProUGUI interactText; // Ttxt_G_04
    [SerializeField] private TextMeshProUGUI settingText;
    [SerializeField] private TextMeshProUGUI skipText;
    [SerializeField] private TextMeshProUGUI cameraText;
    [SerializeField] private TextMeshProUGUI inspectText;
    [SerializeField] private TextMeshProUGUI leftClickText;
    [SerializeField] private TextMeshProUGUI qSkipText;

    [Header("Click Sound")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip exitClip;



    private Action<OpenControlGuideEvent> _controlGuide;
    private Action<GamePhaseChangedEvent> _phaseChangedHandler;

    private GamePhase _currentPhase;
    public bool IsOpen { get; private set; } = false;

    private void Awake()
    {
        _controlGuide = e => OnOpenGuide();
        _phaseChangedHandler = e => {
            _currentPhase = e.Phase; // 페이즈가 바뀔 때마다 업데이트
        };
    }

    private void Start()
    {
        nextBtn.onClick.AddListener(Next);
        prevBtn.onClick.AddListener(Prev);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_controlGuide);
        EventBus.Subscribe(_phaseChangedHandler);

    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_controlGuide);
        EventBus.Unsubscribe(_phaseChangedHandler);
    }

    private void Update()
    {
        if (rootPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSound();
            Close();
        }
        if (IsOpen == true && Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PlaySound();
            Prev();
        }

        if (IsOpen == true && Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            PlaySound();
            Next();
        }
    }

    public void Next() { if (_currentIndex < pages.Count - 1) { _currentIndex++; UpdateUI(); } }
    public void Prev() { Debug.Log("뒤로가기클릭"); if (_currentIndex > 0) { _currentIndex--; UpdateUI(); } else { Debug.Log("첫 페이지라 이동 불가"); } }

    private void OnOpenGuide()
    {
        if(IsOpen == true)
        {
            Close();
            return;
        }
        IsOpen = true;
        rootPanel.SetActive(true);
        _currentIndex = 0;
        RefreshLocalizedTexts();
        UpdateUI();
        // 마우스 커서 활성화
        if (_currentPhase == GamePhase.Tutorial)
        {
            Time.timeScale = 0f;
            EventBus.Publish(new PauseGameRequestedEvent());
            EventBus.Publish(new CursorOverrideReleasedEvent());

            // Dialogue Raycast 차단
            if (DialogueManager.Instance != null &&
                DialogueManager.Instance.IsDialogueOpen)
            {
                DialogueManager.Instance.SetRaycastBlocked(true);
            }

            EventBus.Publish(new GlobalInputLockRequestedEvent());
        }
        Debug.Log("조작가이드 이벤트 수신");
    }

    private void UpdateUI()
    {
        for (int i = 0; i < pages.Count; i++) pages[i].SetActive(i == _currentIndex);
        prevBtn.interactable = (_currentIndex > 0);
        nextBtn.interactable = (_currentIndex < pages.Count - 1);
    }

    public void Close()
    {
        IsOpen = false;
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
            if (_currentPhase == GamePhase.Tutorial)
            {
                Time.timeScale = 1f;
                EventBus.Publish(new ResumeGameRequestedEvent());
                // =========================
                // Dialogue Raycast 복구
                // =========================
                if (DialogueManager.Instance != null &&
                    DialogueManager.Instance.IsDialogueOpen)
                {
                    DialogueManager.Instance.SetRaycastBlocked(false);
                }
                // 반드시 UI Lock 해제
                EventBus.Publish(new GlobalInputLockReleasedEvent());
            }
        }
    }

    private void RefreshLocalizedTexts()
    {
        // 기획자 CSV의 TutorialTextID 열에 적힌 값 그대로 사용
        if (moveText != null) moveText.text = TextManager.Instance.GetTutorialText("Ttxt_G_01");
        if (dashText != null) dashText.text = TextManager.Instance.GetTutorialText("Ttxt_G_02");
        if (sitText != null) sitText.text = TextManager.Instance.GetTutorialText("Ttxt_G_03");
        if (interactText != null) interactText.text = TextManager.Instance.GetTutorialText("Ttxt_G_04");
        if (settingText != null) settingText.text = TextManager.Instance.GetTutorialText("Ttxt_G_05");
        if (skipText != null) skipText.text = TextManager.Instance.GetTutorialText("Ttxt_G_06");
        if (cameraText != null) cameraText.text = TextManager.Instance.GetTutorialText("Ttxt_G_07");
        if (inspectText != null) inspectText.text = TextManager.Instance.GetTutorialText("Ttxt_G_08");
        if (leftClickText != null) leftClickText.text = TextManager.Instance.GetTutorialText("Ttxt_G_09");
        if (qSkipText != null) qSkipText.text = TextManager.Instance.GetTutorialText("Ttxt_G_10");
    }

    private void PlaySound()
    {
        if (clickClip == null)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayUISound(clickClip);
    }

    private void ExitSound()
    {
        if (exitClip == null)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayUISound(exitClip);
    }

}
