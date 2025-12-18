using UnityEngine;
using UnityEngine.Rendering;

public class InspectionUIController : MonoBehaviour
{
    [SerializeField] private GameObject inspectionRoot;
    [SerializeField] private Volume inspectionBlurVolume;

    private void Awake()
    {
        // 기본 상태: 블러 꺼짐
        inspectionBlurVolume.weight = 0f;
        inspectionRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<InspectionStartedEvent>(OnInspectionStarted);
        EventBus.Subscribe<InspectionEndedEvent>(OnInspectionEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<InspectionStartedEvent>(OnInspectionStarted);
        EventBus.Unsubscribe<InspectionEndedEvent>(OnInspectionEnded);
    }

    private void OnInspectionStarted(InspectionStartedEvent e)
    {
        inspectionRoot.SetActive(true);
        inspectionBlurVolume.weight = 1f;
    }

    private void OnInspectionEnded(InspectionEndedEvent e)
    {
        inspectionBlurVolume.weight = 0f;
        inspectionRoot.SetActive(false);
    }
}

