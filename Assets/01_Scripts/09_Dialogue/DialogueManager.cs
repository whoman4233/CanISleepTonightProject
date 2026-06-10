using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue Components")]
    [SerializeField] private GameObject dialoguePanel; // 대화 UI 패널
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueContentText;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.05f; // 타이핑 속도 (추후 조절 가능)

    [Header("Current Dialogue")]
    private string _currentDialogueKey;
    private string _currentSpeakerNameRaw;
    private string _currentDialogueRaw;

    private float _lastOpenTime;
    private bool _isFirstInputGuard = false;

    // =========================
    // Raycast 제어용 CanvasGroup
    // =========================
    [Header("Raycast Control")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    private Queue<DialogueLine> dialogueQueue;
    private Coroutine dialogueRoutine;
    private bool isTyping = false;
    private DialogueLine currentLine; // 한번에 문장표기 전용

    private PlayerInputs.DialogueActions _dialogueActions; //Dialogue용 액션맵 추가
    private bool _allowContinueInput; // 입력 허용 여부

    private Action _onDialogueComplete;

    // =========================
    // WaitForSecondsRealtime 캐싱
    // - PauseGameRequestedEvent로 Time.timeScale = 0 이 되어도
    //   Dialogue 타이핑/딜레이가 멈추지 않도록 하기 위함
    // =========================
    private readonly Dictionary<float, WaitForSecondsRealtime> _waitRealtimeCache
        = new Dictionary<float, WaitForSecondsRealtime>();

    private bool canClick = false; // 문장 씹힘 방지
    public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf; //미션 브리핑/결과용 프로퍼티

    // =========================
    // [추가] 대화 진입 시 "E 잔여 입력" 제거용 코루틴 핸들
    // =========================
    private Coroutine _waitReleaseRoutine; // [추가]

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        dialogueQueue = new Queue<DialogueLine>();
        dialoguePanel.SetActive(false);
        // =========================
        // 기본 Raycast 허용
        // =========================
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.blocksRaycasts = true;
            dialogueCanvasGroup.interactable = true;
        }
    }
    // =========================
    // PauseMenu 연동용 API
    // =========================
    public void SetRaycastBlocked(bool blocked)
    {
        if (dialogueCanvasGroup == null)
            return;

        // PauseMenu가 열리면 Dialogue는 입력만 차단
        dialogueCanvasGroup.blocksRaycasts = !blocked;
        dialogueCanvasGroup.interactable = !blocked;
    }
    private void OnEnable()
    {
        //  Dialogue ActionMap 입력 구독
        if (InputManager.Instance != null)
        {
            _dialogueActions = InputManager.Instance.Inputs.Dialogue;

            _dialogueActions.Continue.started += OnDialogueContinueStarted;

            // =========================
            // [추가] "키업" 감지를 위해 canceled도 구독
            // =========================
            _dialogueActions.Continue.canceled += OnDialogueContinueCanceled; // [추가]

            _dialogueActions.Skip.started += OnDialogueSkip; // Skip은 단발 입력

            TextManager.OnLanguageChanged += RefreshCurrentDialogue;
        }
    }

    private void OnDisable()
    {
        // Dialogue ActionMap 입력 해제
        if (InputManager.Instance == null)
            return;

        _dialogueActions.Continue.started -= OnDialogueContinueStarted;

        // =========================
        // [추가] canceled 해제
        // =========================
        _dialogueActions.Continue.canceled -= OnDialogueContinueCanceled; // [추가]

        _dialogueActions.Skip.started -= OnDialogueSkip;

        TextManager.OnLanguageChanged -= RefreshCurrentDialogue;
    }

    // =========================
    // Dialogue Input Callbacks
    // =========================
    private void OnDialogueContinueStarted(InputAction.CallbackContext ctx)
    {
        if (!_allowContinueInput)
            return;

        OnContinueClicked();
    }

    // =========================
    // [추가] 키업이 한번이라도 발생하면,
    // 이제부터는 "새로운 입력"만 들어온다고 볼 수 있음
    // =========================
    private void OnDialogueContinueCanceled(InputAction.CallbackContext ctx) // [추가]
    {
        // 키업이 확인된 이후부터 입력 허용
        _allowContinueInput = true;
    }

    private void OnDialogueSkip(InputAction.CallbackContext ctx)
    {
        //  전체 대화 스킵
        SkipAllDialogue();
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

    // =========================
    // Dialogue 공통 진입 처리
    // =========================
    private void EnterDialogueMode()
    {
        _lastOpenTime = Time.unscaledTime; // 시간 저장
        _isFirstInputGuard = true; // 대화 시작 시 가드 활성화
        EventBus.Publish(new DialogueStartedEvent());
        EventBus.Publish(new CursorOverrideRequestedEvent
        {
            HideCursor = true,
            LockMode = CursorLockMode.Locked
        });

        if (InputManager.Instance == null)
            return;

        InputManager.Instance.SetDialogueActive(true);

        // Player 입력 차단
        InputManager.Instance.Inputs.Player.Disable();

        // Dialogue 입력 강제 리셋
        var dialogueMap = InputManager.Instance.Inputs.Dialogue;
        dialogueMap.Disable();
        dialogueMap.Enable();

        // =========================
        // [핵심 수정] 대화 진입 직후에는 Continue 입력을 무조건 막고,
        // "E 키업(Release)"가 감지될 때까지 기다린다.
        // =========================
        _allowContinueInput = false; // [수정]
        canClick = false;

        if (_waitReleaseRoutine != null) // [추가]
        {
            StopCoroutine(_waitReleaseRoutine);
            _waitReleaseRoutine = null;
        }

        _waitReleaseRoutine = StartCoroutine(WaitContinueReleaseThenEnable()); // [추가]
    }

    // =========================
    // [추가] Continue가 눌린 상태로 들어오는 경우(상호작용 E 잔여)
    // 릴리즈 될 때까지 기다렸다가 그 이후부터 입력 허용
    // =========================
    private IEnumerator WaitContinueReleaseThenEnable() // [추가]
    {
        // 한 프레임은 무조건 넘겨서, EnterDialogueMode 진입 프레임의 입력을 제거
        yield return null;

        // 눌려있는 동안 대기
        while (_dialogueActions.Continue.IsPressed())
            yield return null;

        // 릴리즈 확인 후 허용
        _allowContinueInput = true;
        _waitReleaseRoutine = null;
    }

    public void StartDialogue(DialogueData data) //NPC가 대화를 시작할 때 호출하는 진입점
    {
        if (data == null || data.Lines == null || data.Lines.Length == 0)
        {
            Debug.LogWarning("대화 데이터가 비어있습니다.");
            return;
        }
        if (dialoguePanel.activeSelf) return; // 이미 대화중이면 무시

        // 공통 진입 처리
        EnterDialogueMode();

        dialogueQueue.Clear();
        foreach (DialogueLine line in data.Lines) // 큐 초기화 및 데이터 로드
        {
            dialogueQueue.Enqueue(line);
        }

        dialoguePanel.SetActive(true);
        DisplayNextLine();
        Debug.Log("StartDialogue");
    }
    public void StartDialogueByKey(string textKey, Action onComplete = null) // 미션04를 위한 단일 키 실행 메서드
    {
        if (dialoguePanel.activeSelf)
            return;

        _onDialogueComplete = onComplete;

        EnterDialogueMode();

        dialogueQueue.Clear();
        dialogueQueue.Enqueue(new DialogueLine { textKey = textKey });

        dialoguePanel.SetActive(true);
        DisplayNextLine();
    }

    //미션 대사용
    public void StartDialogue(DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        if (dialoguePanel.activeSelf)
            return;

        // [수정] 공통 진입 처리
        EnterDialogueMode();

        dialogueQueue.Clear();
        foreach (var line in lines)
            dialogueQueue.Enqueue(line);

        dialoguePanel.SetActive(true);
        DisplayNextLine();
    }

    private void DisplayNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = dialogueQueue.Dequeue();

        // =========================
        // 현재 대화 Key 저장 (언어 변경 대응용)
        // =========================
        _currentDialogueKey = currentLine.textKey; 

        var entry = TextManager.Instance.GetEntry(_currentDialogueKey); // Entry 직접 다시 조회
        if (entry == null)
        {
            Debug.LogWarning("대사가 없읍니다.");
            return;
        }

        // =========================
        // raw 데이터 캐싱 (언어 변경 대응용)
        // =========================
        _currentSpeakerNameRaw = entry.speaker;
        _currentDialogueRaw = TextManager.Instance.GetText(_currentDialogueKey);

        // =========================
        // 치환 처리
        // =========================
        if (DailyMissionManager.Instance != null &&
            DailyMissionManager.Instance.CurrentMission != null)
        {
            var strategy = DailyMissionManager.Instance.CurrentMission;

            _currentSpeakerNameRaw =
                strategy.GetProcessedText(_currentSpeakerNameRaw);

            _currentDialogueRaw =
                strategy.GetProcessedText(_currentDialogueRaw);
        }

        // =========================
        // 문자열 직접 사용하지 않고 캐싱값 사용
        // =========================
        speakerNameText.text = _currentSpeakerNameRaw;

        // 타이핑 시작
        ResetRoutine();
        isTyping = true;
        canClick = false;

        dialogueRoutine =
            StartCoroutine(TypeSentenceRealtime(_currentDialogueRaw)); // raw 사용
    }

    // =========================
    // Realtime 기반 타이핑 루틴
    // - Pause 상태에서도 Dialogue UX 유지
    // =========================
    private IEnumerator TypeSentenceRealtime(string sentance)
    {
        isTyping = true;
        dialogueContentText.text = sentance;
        dialogueContentText.maxVisibleCharacters = 0;

        int totalChars = sentance.Length;
        var wait = GetWaitRealtime(typingSpeed);

        for (int i = 0; i <= totalChars; i++)
        {
            dialogueContentText.maxVisibleCharacters = i;
            yield return wait;
        }

        isTyping = false;
        canClick = true;
        dialogueRoutine = null;
        // 처음에 모든 문장을 text에 넣고 maxVisibleCharacters = i인 i값에 따라 문자 랜더링 개수만 바꿔준다. 메모리 최적화
    }

    public void EndDialogue()
    {
        EventBus.Publish(new DialogueEndedEvent());
        EventBus.Publish(new CursorOverrideReleasedEvent());
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

        // =========================
        // 종료 시 Raycast 복구
        // =========================
        SetRaycastBlocked(false);

        speakerNameText.text = string.Empty;
        dialogueContentText.text = string.Empty;
        dialogueContentText.maxVisibleCharacters = 0;

        if (_onDialogueComplete != null)
        {
            var callback = _onDialogueComplete;
            _onDialogueComplete = null; // 중복 실행 방지
            callback.Invoke();
        }
        _isFirstInputGuard = false;
    }

    public void OnContinueClicked()
    {
        if (_isFirstInputGuard)
        {
            if (Time.unscaledTime - _lastOpenTime < 0.25f) // 0.25초 이내에 들어온 입력은 싹 다 무시함. 최초 1회
            {
                return; // 던지기/상호작용 잔여 입력 무시
            }
            _isFirstInputGuard = false; // 가드 해제
        }
        // 타이핑 중이면 canClick과 무관하게 처리
        if (isTyping)
        {
            StopCoroutine(dialogueRoutine);
            dialogueContentText.maxVisibleCharacters =
                dialogueContentText.text.Length;

            isTyping = false;
            canClick = true;
            return;
        }

        // 타이핑이 끝난 뒤에만 다음 대사 허용
        if (!canClick)
            return;

        DisplayNextLine();
    }

    // =========================
    // Realtime Wait 캐시
    // =========================
    private WaitForSecondsRealtime GetWaitRealtime(float time)
    {
        if (!_waitRealtimeCache.TryGetValue(time, out var wait))
        {
            wait = new WaitForSecondsRealtime(time);
            _waitRealtimeCache.Add(time, wait);
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

    public void StartDialogueByKeys(string speakerKey, string textType = "Dialogue", Action onComplete = null)
    {
        EnterDialogueMode();
        if (dialoguePanel.activeSelf) return; // 이미 대화 중이면 무시
        _onDialogueComplete = onComplete;

        string missionId = "";

        // 미션 ID 판정 로직
        if (GameManager.Instance.CurrentPhase == GamePhase.Tutorial)
        {
            missionId = DialogueKeys.Missions.Tutorial;
        }
        else
        {
            // 튜토리얼이 아닐 때는 미션 정보가 필수
            if (DailyMissionManager.Instance.CurrentMission != null)
            {
                missionId = DailyMissionManager.Instance.CurrentMission.missionId;
            }
            else
            {
                _onDialogueComplete?.Invoke();
                _onDialogueComplete = null;
                // 미션 정보도 없고 튜토리얼도 아니면 대화를 할 수 없음
                Debug.LogWarning("[Dialogue] 현재 활성화된 미션이 없습니다.");
                return;
            }
        }

        // TextManager에게 키 리스트 요청
        List<string> keys = TextManager.Instance.GetKeysByMissionAndSpeaker(
            missionId, speakerKey, textType
        );

        if (keys == null || keys.Count == 0)
        {
            Debug.Log($"[Dialogue] 대사를 찾을 수 없음: Mission={missionId}, Speaker={speakerKey}, Type={textType}");
            EndDialogue();
            return;
        }

        // 입력 제어 및 UI 활성화
        EnterDialogueMode();

        dialogueQueue.Clear();
        foreach (string key in keys)
        {
            DialogueLine line = new DialogueLine { textKey = key };
            dialogueQueue.Enqueue(line);
        }

        dialoguePanel.SetActive(true);
        DisplayNextLine();
    }
    private void RefreshCurrentDialogue()
    {
        if (!IsDialogueOpen)
            return;

        if (string.IsNullOrEmpty(_currentDialogueKey))
            return;

        var entry = TextManager.Instance.GetEntry(_currentDialogueKey);
        if (entry == null)
            return;

        _currentSpeakerNameRaw = entry.speaker;
        _currentDialogueRaw =
            TextManager.Instance.GetText(_currentDialogueKey);

        if (DailyMissionManager.Instance != null &&
            DailyMissionManager.Instance.CurrentMission != null)
        {
            var strategy = DailyMissionManager.Instance.CurrentMission;

            _currentSpeakerNameRaw =
                strategy.GetProcessedText(_currentSpeakerNameRaw);

            _currentDialogueRaw =
                strategy.GetProcessedText(_currentDialogueRaw);
        }

        speakerNameText.text = _currentSpeakerNameRaw;

        // 타이핑 중이면 전체 표시로 전환
        if (isTyping)
        {
            StopCoroutine(dialogueRoutine);
            isTyping = false;
        }

        dialogueContentText.text = _currentDialogueRaw;
        dialogueContentText.maxVisibleCharacters =
            dialogueContentText.text.Length;
    }
}


