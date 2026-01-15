using System;
using UnityEngine;

public class PromptUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject interactPanel;
    [SerializeField] private TMPro.TMP_Text interactText;

    [SerializeField] private GameObject inspectionPanel;
    [SerializeField] private TMPro.TMP_Text inspectionText;

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

        interactText.text =
            TextManager.Instance.GetPromptText(id);

        interactPanel.SetActive(true);
        inspectionPanel.SetActive(false);
    }

    private void UpdateInspectionPrompt(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            inspectionPanel.SetActive(false);
            return;
        }

        inspectionText.text =
            TextManager.Instance.GetPromptText(id);

        inspectionPanel.SetActive(true);
        interactPanel.SetActive(false);
    }
}
