using UnityEngine;
using TMPro;
using System;

/// <summary>
/// HUD 미션 진행도 패널
/// - 텍스트 라벨은 MissionTextBinder가 담당
/// - HUDMissionPanel은 숫자(current / target)만 덧붙인다
/// </summary>
public class HUDMissionPanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Progress Text")]
    [SerializeField] private TextMeshProUGUI progressText;

    // MissionTextBinder가 세팅한 라벨 캐시
    private string _progressLabel;

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
    /// 미션 공개 시
    /// - 패널 활성화
    /// - MissionTextBinder가 세팅한 라벨을 캐싱
    /// </summary>
    private void OnMissionRevealed(MissionRevealedEvent e)
    {
        if (panelRoot == null || progressText == null)
            return;

        panelRoot.SetActive(true);

        // MissionTextBinder가 이미 세팅한 텍스트를 라벨로 저장
        _progressLabel = progressText.text.Trim();

        // 초기 진행도 표시
        UpdateProgressText(0, e.mission.targetScore);
    }

    /// <summary>
    /// 진행도 변경 이벤트
    /// </summary>
    private void OnMissionProgressChanged(MissionProgressChangedEvent e)
    {
        UpdateProgressText(e.current, e.target);
    }

    private void OnUIHardReset(UIHardResetEvent e)
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// 숫자만 갱신
    /// </summary>
    private void UpdateProgressText(int current, int target)
    {
        if (progressText == null)
            return;

        if (string.IsNullOrEmpty(_progressLabel))
        {
            // 예외 방어: 라벨이 아직 없을 경우 현재 텍스트를 기준으로 삼음
            _progressLabel = progressText.text.Trim();
        }

        progressText.text = $"{_progressLabel} {current} / {target}";
    }
}






