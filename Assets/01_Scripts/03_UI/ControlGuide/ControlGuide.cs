using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        if (rootPanel.activeSelf && Input.GetKeyDown(KeyCode.Q))
        {
            Close();
        }
    }

    public void Next() { if (_currentIndex < pages.Count - 1) { _currentIndex++; UpdateUI(); } }
    public void Prev() { Debug.Log("뒤로가기클릭"); if (_currentIndex > 0) { _currentIndex--; UpdateUI(); } else { Debug.Log("첫 페이지라 이동 불가"); } }

    private void OnOpenGuide()
    {
        IsOpen = true;
        rootPanel.SetActive(true);
        _currentIndex = 0;
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

}
