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
    private readonly Dictionary<float, WaitForSeconds> _waitCache = new Dictionary<float, WaitForSeconds>(); // WaitForSeconds 캐싱 (GC 최적화)
    private bool canClick = false; // 문장 씹힘 방지

    private void Awake()
    {
        dialogueQueue = new Queue<DialogueLine>();
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        // 대화 중일 때만 Q키 체크
        if (dialoguePanel.activeSelf && Input.GetKeyDown(KeyCode.Q))
        {
            SkipAllDialogue();
        }
    }

    public void SkipAllDialogue()
    {
        // 현재 타이핑 중인 루틴 중지
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        // 대화 큐 완전 비우기
        dialogueQueue.Clear();

        // 루틴 및 상태 리셋 후 종료
        isTyping = false;
        EndDialogue();

        Debug.Log("전체 대화 스킵");
    }

    public void StartDialogue(DialogueData data) //NPC가 대화를 시작할 때 호출하는 진입점
    {
        if (data == null || data.Lines == null || data.Lines.Length == 0)
        {
            Debug.LogWarning("대화 데이터가 비어있습니다.");
            return;
        }
        if (dialoguePanel.activeSelf) return; // 이미 대화중이면 무시
        if (InputManager.Instance != null)
            InputManager.Instance.SetDialogueActive(true);
        dialogueQueue.Clear();
        foreach (DialogueLine line in data.Lines) // 큐 초기화 및 데이터 로드
        {
            dialogueQueue.Enqueue(line);
        }
        if (InputManager.Instance != null) // 입력 초기화
        {
            InputManager.Instance.ResetPlayerInputs();
        }
        dialoguePanel.SetActive(true);
        canClick = false;
        DisplayNextLine();
        StartCoroutine(EnableNextDelay(0.2f));
        Debug.Log("StartDialogue");
    }

    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = dialogueQueue.Dequeue();

        var entry = currentLine.Entry;
        if (entry == null)
        {
            Debug.LogWarning("대사가 없읍니다.");
            return;
        }
        string speakerName = entry.speaker;
        string dialogueContent = currentLine.TranslatedContent;

        if (DailyMissionManager.Instance != null && DailyMissionManager.Instance.CurrentMission != null)
        {
            var strategy = DailyMissionManager.Instance.CurrentMission;
            speakerName = strategy.GetProcessedText(speakerName);
            dialogueContent = strategy.GetProcessedText(dialogueContent);
        }

        speakerNameText.text = speakerName; // 치환된 이름 적용

        // 타이핑 시작
        ResetRoutine();
        isTyping = true;
        dialogueRoutine = StartCoroutine(TypeSentence(dialogueContent)); // 치환된 내용 적용

    }

    private IEnumerator TypeSentence(string sentance)  // 타이핑 루틴
    {
        
        isTyping = true;
        dialogueContentText.text = sentance;
        dialogueContentText.maxVisibleCharacters = 0;
        int totalChars = sentance.Length;
        var wait = GetWait(typingSpeed);
        for (int i = 0; i <= totalChars; i++)
        {
            dialogueContentText.maxVisibleCharacters = i;
            yield return wait;
        }
        isTyping = false;
        dialogueRoutine = null;
        // 처음에 모든 문장을 text에 넣고 maxVisibleCharacters = i인 i값에 따라 문자 랜더링 개수만 바꿔준다. 메모리 최적화
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        ResetRoutine();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetDialogueActive(false);
        }
        speakerNameText.text = string.Empty; // 이름 지워주고
        dialogueContentText.text = string.Empty; // 텍스트 지워주고
        dialogueContentText.maxVisibleCharacters = 0; // 텍스트 숫자 0으로만들어주기
        Debug.Log("End Dialogue");
    }

    public void OnContinueClicked()  // 플레이어가 클릭하면 다음 대화 호출
    {
        if (!canClick) return;
        if (isTyping)
        {
            StopCoroutine(dialogueRoutine);
            //DialogueLine currentLine = dialogueQueue.Dequeue();
            //dialogueContentText.text = dialogueQueue.Peek().Text;
            //dialogueContentText.text = currentLine.Text;
            dialogueContentText.maxVisibleCharacters = dialogueContentText.text.Length;
            isTyping = false;
        }
        else
        {
            DisplayNextLine();
        }
    }
    private WaitForSeconds GetWait(float time)
    {
        if (!_waitCache.TryGetValue(time, out var wait))
        {
            wait = new WaitForSeconds(time);
            _waitCache.Add(time, wait);
        }
        return wait;
    }
    private void ResetRoutine()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }
    }
    private IEnumerator EnableNextDelay(float delay)
    {
        yield return GetWait(delay);
        canClick = true;
    }
    public void StartDialogueByKeys(string speakerKey, string textType = "Dialogue")
    {
        if (DailyMissionManager.Instance.CurrentMission == null) return;
        if (dialoguePanel.activeSelf) return; // 중복 실행 방지

        string missionId = DailyMissionManager.Instance.CurrentMission.missionId; // 현재 미션 정보 가져오기

        // TextManager에게 해당 조건의 키 리스트 요청
        List<string> keys = TextManager.Instance.GetKeysByMissionAndSpeaker(missionId, speakerKey, textType);

        if (keys == null || keys.Count == 0) return;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.SetDialogueActive(true);
            InputManager.Instance.ResetPlayerInputs();
        }
        // 대화 큐 생성 로직
        dialogueQueue.Clear();
        foreach (string key in keys)
        {
            DialogueLine line = new DialogueLine { textKey = key };
            dialogueQueue.Enqueue(line);
        }

        // 4. UI 활성화
        dialoguePanel.SetActive(true);
        DisplayNextLine();
        StartCoroutine(EnableNextDelay(0.2f));
    }
}
