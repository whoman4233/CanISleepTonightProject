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

    private Action<InspectionViewRequestedEvent> _onViewRequested;
    private Action<InspectionViewReleasedEvent> _onViewReleased;

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
        inspectionRoot.SetActive(true);
        inspectionBlurVolume.weight = 1f;

        // 즉시 보내지 말고
        StartCoroutine(NotifyViewReadyNextFrame());
    }

    private IEnumerator NotifyViewReadyNextFrame()
    {
        yield return null; // 다음 프레임 보장

        EventBus.Publish(new InspectionViewReadyEvent
        {
            ViewRect = inspectionRawImage.rectTransform
        });
    }


    private void OnViewReleased(InspectionViewReleasedEvent e)
    {
        inspectionRoot.SetActive(false);
        inspectionBlurVolume.weight = 0f;
    }
}



