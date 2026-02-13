using System;
using UnityEngine;

public class PromptUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject interactPanel;
    [SerializeField] private LocalizedText interactText;

    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private LocalizedText inspectionText;

    private Action<PromptChangedEvent> _onPrompt;

    private void Awake()
    {
        _onPrompt = OnPromptChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPrompt);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPrompt);
    }

    private void OnPromptChanged(PromptChangedEvent e)
    {
        switch (e.context)
        {
            case PromptContext.Interact:
                UpdateInteractPrompt(e.promptId);
                break;

            case PromptContext.Inspection:
                UpdateInspectionPrompt(e.promptId);
                break;
        }
    }

    private void UpdateInteractPrompt(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            interactPanel.SetActive(false);
            return;
        }

        if (inspectionPanel.activeSelf)
            return;

        // 문자열 직접 세팅 제거
        interactText.SetRuntimeId(id);
        interactPanel.SetActive(true);
    }

    private void UpdateInspectionPrompt(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            inspectionPanel.SetActive(false);
            return;
        }

        inspectionText.SetRuntimeId(id);

        inspectionPanel.SetActive(true);
        interactPanel.SetActive(false);
    }
}

