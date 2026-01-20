using System;
using TMPro;
using UnityEngine;

public class DialoguePanelView : MonoBehaviour, IDialogueView
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueContentText;

    public bool IsOpen => dialoguePanel != null && dialoguePanel.activeSelf;

    // =========================
    // UIHardReset 수신
    // =========================
    private Action<UIHardResetEvent> _onUIHardReset;

    private void Awake()
    {
        _onUIHardReset = OnUIHardReset;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onUIHardReset);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onUIHardReset);
    }

    // =========================
    // Hard Reset 처리
    // =========================
    private void OnUIHardReset(UIHardResetEvent e)
    {
        Hide();
    }

    public void Show()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
    }

    public void Hide()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // =========================
        // 텍스트 정리
        // =========================
        if (speakerNameText != null)
            speakerNameText.text = string.Empty;

        if (dialogueContentText != null)
        {
            dialogueContentText.text = string.Empty;
            dialogueContentText.maxVisibleCharacters = 0;
        }
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
