using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{

    [Header("Dialogue Components")]
    [SerializeField] private GameObject dialoguePanel; // 대화 UI 패널
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueContentText;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f; // 타이핑 속도 (추후 조절 가능)

    private Queue<DialogueLine> dialogueQueue;
    private Coroutine dialogueRoutine;
    private bool isTyping = false;
    private DialogueLine currentLine; // 한번에 문장표기 전용

    private void Awake()
    {
        dialogueQueue = new Queue<DialogueLine>();
        dialoguePanel.SetActive(false);
    }

    public void StartDialogue(DialogueData data) //NPC가 대화를 시작할 때 호출하는 진입점
    {
        if (dialoguePanel.activeSelf) return; // 이미 대화중이면 무시
        if (data == null || data.Lines == null || data.Lines.Length == 0)
        {
            Debug.LogWarning("대화 데이터가 비어있습니다.");
            return;
        }
        if (InputManager.Instance != null)
            InputManager.Instance.SetDialogueActive(true);
        dialogueQueue.Clear();
        foreach (DialogueLine line in data.Lines) // 큐 초기화 및 데이터 로드
        {
            dialogueQueue.Enqueue(line);
        }
        dialoguePanel.SetActive(true);
        DisplayNextLine();
        Debug.Log("StartDialogue");
    }

    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0) // 남은대화 없으면
        {
            EndDialogue(); // 종료 매서드 실행
            return;
        }
        DialogueLine nextLine = dialogueQueue.Dequeue();
        currentLine = nextLine;
        speakerNameText.text = nextLine.SpeakerName;

        if (dialogueRoutine != null) StopCoroutine(dialogueRoutine);
        dialogueRoutine = StartCoroutine(TypeSentence(currentLine.Text));
        //dialogueRoutine = StartCoroutine(TypeSentence(line.Text));

    }

    private IEnumerator TypeSentence(string sentance)  // 타이핑 루틴
    {
        isTyping = true;
        dialogueContentText.text = "";
        foreach (char letter in sentance.ToCharArray())
        {
            dialogueContentText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        speakerNameText.text = "";
        dialogueContentText.text = "";
        if (InputManager.Instance != null)
            InputManager.Instance.SetDialogueActive(false);
        Debug.Log("End Dialogue");
    }

    public void OnContinueClicked()  // 플레이어가 클릭하면 다음 대화 호출
    {
        if (isTyping)
        {
            StopCoroutine(dialogueRoutine);
            //DialogueLine currentLine = dialogueQueue.Dequeue();
            //dialogueContentText.text = dialogueQueue.Peek().Text;
            dialogueContentText.text = currentLine.Text;
            isTyping = false;
        }
        else
        {
            DisplayNextLine();
        }
    }
}
