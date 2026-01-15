using System;
using TMPro;
using UnityEngine;

public class HUDMissionPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI targetText;

    private Action<MissionRevealedEvent> _onMissionRevealed;
    private Action<MissionProgressChangedEvent> _onProgress;
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onMissionRevealed = OnMissionRevealed;
        _onProgress = OnMissionProgressChanged;
        _onUIHardReset = OnUIHardReset;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onMissionRevealed);
        EventBus.Subscribe(_onProgress);
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onMissionRevealed);
        EventBus.Unsubscribe(_onProgress);
        EventBus.Unsubscribe(_onUIHardReset);
    }

    /// <summary>
    /// 미션 시작 / 공개 시
    /// </summary>
    private void OnMissionRevealed(MissionRevealedEvent e)
    {
        if (panelRoot == null || targetText == null)
            return;

        panelRoot.SetActive(true);

        // 하루 기준 초기화
        UpdateTargetText(0, e.mission.targetScore);
    }

    /// <summary>
    /// 진행도 갱신
    /// </summary>
    private void OnMissionProgressChanged(MissionProgressChangedEvent e)
    {
        UpdateTargetText(e.current, e.target);
    }

    private void OnUIHardReset(UIHardResetEvent e)
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (targetText != null)
            targetText.text = string.Empty;
    }

    /// <summary>
    /// 숫자 전용 출력
    /// </summary>
    private void UpdateTargetText(int current, int target)
    {
        targetText.text = $"{current} / {target}";
    }
}







