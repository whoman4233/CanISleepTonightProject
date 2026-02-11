using System;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenuController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnReturnToTitle;
    [SerializeField] private Button btnOptions;
    [SerializeField] private Button btnGuide;
    [Header("UI Sounds")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    private bool isOpen;

    [Header("Control Guide")]
    [SerializeField] private ControlGuide _controlGuide;

    private Action<PauseMenuOpenRequestedEvent> _onOpenRequested;
    private Action<PauseMenuCloseRequestedEvent> _onCloseRequested;

    private void Awake()
    {
        if (menuRoot != null) menuRoot.SetActive(false);

        if (btnResume != null) btnResume.onClick.AddListener(OnClickResume);
        if (btnReturnToTitle != null) btnReturnToTitle.onClick.AddListener(OnClickReturnToTitle);
        if (btnOptions != null) btnOptions.onClick.AddListener(OnClickOptions);
        if (btnGuide != null) btnGuide.onClick.AddListener(OnClickGuide);

        _onOpenRequested = OnPauseMenuOpenRequested;
        _onCloseRequested = OnPauseMenuCloseRequested;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onOpenRequested);
        EventBus.Subscribe(_onCloseRequested);
        SetOpen(false);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onOpenRequested);
        EventBus.Unsubscribe(_onCloseRequested);
    }
    private void OnPauseMenuOpenRequested(PauseMenuOpenRequestedEvent e)
    {
        if (_controlGuide.IsOpen == true) return;
        SetOpen(true);
    }

    private void OnPauseMenuCloseRequested(PauseMenuCloseRequestedEvent e)
    {
        if (_controlGuide.IsOpen == true) return;
        SetOpen(false);
    }
    private void OnClickResume()
    {
        SetOpen(false);
    }

    private void OnClickReturnToTitle()
    {
        SetOpen(false);

        EventBus.Publish(new InspectionEndedEvent());
        EventBus.Publish(new InspectionViewReleasedEvent());

        EventBus.Publish(new ResumeGameRequestedEvent());
        EventBus.Publish(new UIHardResetEvent());
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }


    private void OnClickOptions()
    {
        EventBus.Publish(new ShowSettingsPopupEvent());
    }
    private void OnClickGuide()
    {
        EventBus.Publish(new OpenControlGuideEvent());
        Debug.Log("조작가이드 이벤트 발행");
    }

    private void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;

        if (menuRoot != null)
            menuRoot.SetActive(open);

        if (open)
        {
            Time.timeScale = 0f;

            AudioManager.Instance?.PlayUISound(openClip);
            EventBus.Publish(new PauseGameRequestedEvent());
            // =========================
            // Dialogue가 걸어둔 Cursor Override 해제
            // =========================
            EventBus.Publish(new CursorOverrideReleasedEvent());

            // Dialogue Raycast 차단
            if (DialogueManager.Instance != null &&
                DialogueManager.Instance.IsDialogueOpen)
            {
                DialogueManager.Instance.SetRaycastBlocked(true);
            }

            EventBus.Publish(new GlobalInputLockRequestedEvent());
            EventBus.Publish(new PauseMenuOpenedEvent());
        }
        else
        {
            Time.timeScale = 1f;

            AudioManager.Instance?.PlayUISound(closeClip);
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

            EventBus.Publish(new PauseMenuClosedEvent());
        }
    }

}








