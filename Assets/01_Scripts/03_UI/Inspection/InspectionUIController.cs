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

    [Header("UISound")]
    [SerializeField] private AudioClip onViewClip;
    [SerializeField] private AudioClip offViewClip;

    private Action<InspectionViewRequestedEvent> _onViewRequested;
    private Action<InspectionViewReleasedEvent> _onViewReleased;

    public RectTransform InspectionViewRect => inspectionRawImage.rectTransform;

    private void Awake()
    {
        inspectionRoot.SetActive(false);
        inspectionBlurVolume.weight = 0f;

        _onViewRequested = OnViewRequested;
        _onViewReleased = OnViewReleased;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onViewRequested);
        EventBus.Subscribe(_onViewReleased);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onViewRequested);
        EventBus.Unsubscribe(_onViewReleased);
    }

    private void OnViewRequested(InspectionViewRequestedEvent e)
    {
        AudioManager.Instance.PlayUISound(onViewClip);
        inspectionRoot.SetActive(true);
        inspectionBlurVolume.weight = 1f;

        // 즉시 보내지 말고
        StartCoroutine(NotifyViewReadyNextFrame());
    }

    private IEnumerator NotifyViewReadyNextFrame()
    {
        yield return null; // 다음 프레임 보장
    
        EventBus.Publish(new InspectionViewReadyEvent());
    }


    private void OnViewReleased(InspectionViewReleasedEvent e)
    {
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

}



