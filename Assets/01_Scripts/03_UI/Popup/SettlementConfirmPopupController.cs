using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettlementConfirmPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private Action<ShowSettlementConfirmPopupEvent> _onShowRequested;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onShowRequested = OnShowRequested;
        _onUIHardReset = OnUIHardReset;   

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShowRequested); 
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShowRequested);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    private void OnShowRequested(ShowSettlementConfirmPopupEvent e)
    {
        Show();
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);

        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    private void OnUIHardReset(UIHardResetEvent e)
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        // 먼저 닫고(락/정지 해제), 다음 프레임에 보고 확정 이벤트 발행
        Hide();
        GameManager.Instance.IsTimerPaused = true;
        EventBus.Publish(new SettlementReportConfirmedEvent());
    }

    private void OnCancelClicked()
    {
        Hide();
        GameManager.Instance.IsTimerPaused = false;
    }
}



