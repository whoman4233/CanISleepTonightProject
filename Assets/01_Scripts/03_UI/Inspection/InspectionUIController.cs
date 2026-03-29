using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class InspectionUIController : MonoBehaviour
{
    [SerializeField] private GameObject inspectionRoot;
    [SerializeField] private Volume inspectionBlurVolume;
    [SerializeField] private RawImage inspectionRawImage;

    [Header("UIRender(상세보기) 캔버스")]
    [SerializeField] private Canvas inspectionCanvas;

    [Header("UISound")]
    [SerializeField] private AudioClip onViewClip;
    [SerializeField] private AudioClip offViewClip;

    private Action<InspectionViewRequestedEvent> _onViewRequested;
    private Action<InspectionViewReleasedEvent> _onViewReleased;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    // ★ [추가] 실행 중인 코루틴을 제어하기 위한 변수
    private Coroutine _notifyCoroutine;

    public RectTransform InspectionViewRect => inspectionRawImage.rectTransform;

    public Camera RenderCamera =>
        inspectionCanvas != null ? inspectionCanvas.worldCamera : null;

    private void Awake()
    {
        inspectionRoot.SetActive(false);
        inspectionBlurVolume.weight = 0f;

        _onViewRequested = OnViewRequested;
        _onViewReleased = OnViewReleased;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onViewRequested);
        EventBus.Subscribe(_onViewReleased);
        EventBus.Subscribe(_onPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onViewRequested);
        EventBus.Unsubscribe(_onViewReleased);
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    private void OnViewRequested(InspectionViewRequestedEvent e)
    {
        AudioManager.Instance.PlayUISound(onViewClip);
        inspectionRoot.SetActive(true);
        inspectionBlurVolume.weight = 1f;

        // ★ [수정] 이전 코루틴이 있다면 안전하게 제거
        if (_notifyCoroutine != null) StopCoroutine(_notifyCoroutine);
        _notifyCoroutine = StartCoroutine(NotifyViewReadyNextFrame());
    }

    private IEnumerator NotifyViewReadyNextFrame()
    {
        yield return null; // 다음 프레임 보장

        EventBus.Publish(new InspectionViewReadyEvent());

        // ★ [추가] 할 일 다 했으면 참조 해제
        _notifyCoroutine = null;
    }

    private void OnViewReleased(InspectionViewReleasedEvent e)
    {
        // ★ [핵심 수정] 뷰가 꺼질 때, 아직 이벤트를 안 보냈다면 취소시킴!
        if (_notifyCoroutine != null)
        {
            StopCoroutine(_notifyCoroutine);
            _notifyCoroutine = null;
        }

        AudioManager.Instance.PlayUISound(offViewClip);
        inspectionRoot.SetActive(false);
        inspectionBlurVolume.weight = 0f;
    }

    public RectTransform GetInspectionViewRect()
    {
        return inspectionRawImage != null
            ? inspectionRawImage.rectTransform
            : null;
    }
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        if (e.Phase == GamePhase.Briefing)
        {
            // 이미 꺼져있을 때 중복 실행 방지를 위해 활성화 상태 체크
            if (inspectionRoot.activeSelf)
            {
                OnViewReleased(new InspectionViewReleasedEvent());
                EventBus.Publish(new InspectionEndedEvent());
            }
        }
    }
}