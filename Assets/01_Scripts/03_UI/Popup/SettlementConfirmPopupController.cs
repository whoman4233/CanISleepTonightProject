using UnityEngine;
using UnityEngine.UI;

public class SettlementConfirmPopupController : MonoBehaviour
{
    [SerializeField] private GameObject root;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void Show()
    {
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    private void OnEnable()
    {
        // 입력 잠금
        EventBus.Publish(new GlobalInputLockRequestedEvent());

        // 시간 정지
        EventBus.Publish(new PauseGameRequestedEvent());
    }

    private void OnDisable()
    {
        // 입력 잠금 해제
        EventBus.Publish(new GlobalInputLockReleasedEvent());

        // 시간 재개
        EventBus.Publish(new ResumeGameRequestedEvent());
    }

    private void OnConfirmClicked()
    {
        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Settlement));
        Hide();
    }

    private void OnCancelClicked()
    {
        Hide();
    }
}

