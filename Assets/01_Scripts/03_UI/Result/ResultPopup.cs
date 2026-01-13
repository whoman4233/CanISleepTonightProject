using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPopup : MonoBehaviour
{
    [Header("Content Root (BG + Panel)")]
    [SerializeField] private GameObject contentRoot;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button nextDayButton;

    private Action<ResultUIShowRequestedEvent> _onShow;

    private void Awake()
    {
        _onShow = OnShowRequested;

        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(OnNextDayClicked);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
    }

    private void OnShowRequested(ResultUIShowRequestedEvent e)
    {
        ShowInternal(e.isSuccess, e.failReason);
    }

    private void ShowInternal(bool isSuccess, string failReason)
    {
        contentRoot.SetActive(true);

        if (isSuccess)
        {
            resultTitleText.text = "업무 보고 완료";
            descriptionText.text = "오늘의 임무를 성공적으로 마쳤습니다.";
        }
        else
        {
            resultTitleText.text = "업무 보고 실패";
            descriptionText.text = string.IsNullOrEmpty(failReason)
                ? "목표를 달성하지 못했습니다."
                : failReason;
        }

        InputManager.Instance?.SetDialogueActive(true);
    }

    // 버튼 클릭 시 호출됨
    private void OnNextDayClicked()
    {
        contentRoot.SetActive(false);

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());

        InputManager.Instance?.SetDialogueActive(false);

        EventBus.Publish(new ResultUIConfirmedEvent());
    }
}




