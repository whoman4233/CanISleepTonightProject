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
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onShow = OnShowRequested;
        _onUIHardReset = OnUIHardReset;

        if (nextDayButton != null)
            nextDayButton.onClick.AddListener(OnNextDayClicked);

        if (contentRoot != null)
            contentRoot.SetActive(false);
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

    private void OnShowRequested(ResultUIShowRequestedEvent e)
    {
        ShowInternal(e.isSuccess, e.failReason);
    }

    private void ShowInternal(bool isSuccess, string failReason)
    {
        if (contentRoot != null)
            contentRoot.SetActive(true);

        // =========================
        // ResultPopup은 "정산 UI"이므로 입력/시간 정지 + 커서 보장
        // =========================
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new PauseGameRequestedEvent());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

        //Dialogue 상태는 끔(정산 UI)
        InputManager.Instance?.SetDialogueActive(false);
    }

    private void OnNextDayClicked()
    {
        if (contentRoot != null)
            contentRoot.SetActive(false);

        DailyMissionManager.Instance?.MarkReported();

        EventBus.Publish(new GlobalInputLockReleasedEvent());
        EventBus.Publish(new ResumeGameRequestedEvent());

        //다음날 진입: 씬 재로딩 이벤트
        EventBus.Publish(new RequestSceneReloadEvent());
    }

    private void OnUIHardReset(UIHardResetEvent e)
    {
        if (contentRoot != null)
            contentRoot.SetActive(false);
    }
}







