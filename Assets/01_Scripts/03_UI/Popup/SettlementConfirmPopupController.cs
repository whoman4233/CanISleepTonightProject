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

    private Action<ShowSettlementConfirmPopupEvent> _onShow;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onShow = _ => Show();
        _onUIHardReset = _ => ForceHide(); // 씬전환/리셋 시 잔존 방지

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);

        if (root != null)
            root.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
        EventBus.Unsubscribe(_onUIHardReset);
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

    // 리셋 시 카운트/상태 꼬이지 않도록 강제 비표시
    private void ForceHide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        // =========================
        // 먼저 Hide로 락/일시정지를 해제하고,
        // 한 프레임 뒤에 "보고 확정" 이벤트를 발행
        // =========================
        Hide();
        StartCoroutine(Co_PublishConfirmedNextFrame());
    }

    private IEnumerator Co_PublishConfirmedNextFrame()
    {
        yield return null;
        EventBus.Publish(new SettlementReportConfirmedEvent());
    }

    private void OnCancelClicked()
    {
        Hide();
    }
}


