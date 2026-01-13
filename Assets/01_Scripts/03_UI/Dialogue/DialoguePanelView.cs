using TMPro;
using UnityEngine;

public class DialoguePanelView : MonoBehaviour, IDialogueView
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueContentText;

    public bool IsOpen => dialoguePanel != null && dialoguePanel.activeSelf;

    public void Show()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }

    public void Hide()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    public void SetSpeaker(string speakerName)
    {
        if (speakerNameText != null)
            speakerNameText.text = speakerName ?? string.Empty;
    }

    public void SetContent(string content)
    {
        if (dialogueContentText != null)
            dialogueContentText.text = content ?? string.Empty;
    }

    public void SetMaxVisibleCharacters(int count)
    {
        if (dialogueContentText != null)
            dialogueContentText.maxVisibleCharacters = count;
    }
}
