using System;
using TMPro;
using UnityEngine;

public class ResultUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI resultTitleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Action<ResultUIShowRequestedEvent> _onShow;

    private void Awake()
    {
        root.SetActive(false);
        _onShow = OnShowRequested;
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
        root.SetActive(true);

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

    // 버튼에서 호출
    public void OnNextDayClicked()
    {
        root.SetActive(false);
        InputManager.Instance?.SetDialogueActive(false);

        EventBus.Publish(new ResultUIConfirmedEvent());
    }
}


