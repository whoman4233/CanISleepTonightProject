using System;
using System.Collections.Generic;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    public static TextManager Instance;

    [Header("데이터 참조")]

    // 여러 TextSOData를 동시에 받을 수 있도록 확장
    // - Dialogue용 TextSOData
    // - UI용 TextSOData
    // Inspector에서 여러 개 등록 가능
    [SerializeField] private List<TextSOData> textDataList = new List<TextSOData>();

    [Header("설정")]
    [SerializeField] private Language currentLanguage = Language.Korean;

    // 핵심: 런타임 조회용 딕셔너리
    // Key -> TextEntry 전체
    // (언어별 텍스트 선택은 GetText 시점에 처리)
    private Dictionary<string, TextEntry> textDictionary = new Dictionary<string, TextEntry>();

    [Header("UI / Mission Text Tables")]
    [SerializeField] private UITextTableSO uiTextTable;
    [SerializeField] private MissionTextTableSO missionTextTable;

    [Header("Prompt Text Table")]
    [SerializeField] private UITextTableSO promptTextTable;

    private Dictionary<string, string> _uiTextLookup;
    private Dictionary<string, string> _promptTextLookup;
    /// <summary>
    /// 텍스트 데이터(Dictionary)가 준비되었음을 알리는 이벤트
    /// - 씬에 UI가 먼저 있어도 정상 동기화되도록
    /// </summary>
    public static event Action OnTextDataReady;

    /// <summary>
    /// 언어 변경 알림 이벤트
    /// - 이미 화면에 떠 있는 UI 텍스트 갱신 용도
    /// </summary>
    public static event Action OnLanguageChanged;

    private void Awake()
    {
        // 싱글톤 & DDoL 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDictionary(); // 최초 초기화 (모든 TextSOData 병합)
            BuildUITextCache();       // UI 텍스트
            BuildPromptTextCache(); // Prompt 텍스트
            // 텍스트 시스템 준비 완료 알림
            // UI가 먼저 생성되어도 이 이벤트를 통해 정상 갱신 가능
            OnTextDataReady?.Invoke();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 언어 변경 처리
    /// - Dictionary는 그대로 두고
    /// - GetText 시점에서 언어 분기
    /// </summary>
    public void SetLanguage(Language lang)
    {
        // 같은 언어 재설정 방지
        if (currentLanguage == lang)
            return;

        currentLanguage = lang;

        // Dictionary 재구성은 불필요
        // (TextEntry 자체를 들고 있으므로)
        OnLanguageChanged?.Invoke();

        Debug.Log($"[TextManager] Language changed to: {lang}");
    }

    /// <summary>
    /// 모든 TextSOData를 순회하며 Dictionary를 구성
    /// - Dialogue용 / UI용 TextSOData를 구분하지 않음
    /// - Key 기준으로 단일 조회 테이블 생성
    /// </summary>
    private void InitializeDictionary()
    {
        textDictionary.Clear();

        if (textDataList == null || textDataList.Count == 0)
        {
            Debug.LogError("[TextManager] TextSOData가 하나도 연결되지 않았습니다!");
            return;
        }

        foreach (var textData in textDataList)
        {
            if (textData == null)
                continue;

            foreach (var entry in textData.textList)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                // 중복 키 방지
                if (textDictionary.ContainsKey(entry.key))
                {
                    Debug.LogError($"[TextManager] 중복된 텍스트 키 감지: {entry.key}");
                    continue;
                }

                // TextEntry 전체를 저장
                textDictionary.Add(entry.key, entry);
            }
        }

        Debug.Log($"[TextManager] 텍스트 캐시 완료: {textDictionary.Count}개");
    }

    /// <summary>
    /// Key 기반 TextEntry 조회 (Dialogue / UI 공용)
    /// </summary>
    public TextEntry GetEntry(string key)
    {
        if (textDictionary.TryGetValue(key, out var entry))
        {
            return entry;
        }

        Debug.LogError($"[TextManager] 키를 찾을 수 없음: {key}");
        return null;
    }

    /// <summary>
    /// Key 기반 텍스트 반환 (현재 언어 기준)
    /// </summary>
    public string GetText(string key)
    {
        var entry = GetEntry(key);
        if (entry == null)
            return key;

        return currentLanguage == Language.Korean
            ? entry.ko
            : entry.en;
    }

    /// <summary>
    /// Dialogue 전용 API
    /// - 특정 미션 + 화자 + 타입에 해당하는 모든 대사 Key 반환
    /// </summary>
    public List<string> GetKeysByMissionAndSpeaker(
        string missionId,
        string speakerName,
        string textType
    )
    {
        List<string> resultKeys = new List<string>();

        foreach (var entry in textDictionary.Values)
        {
            if (entry.mission == missionId &&
                entry.speaker == speakerName &&
                entry.type == textType)
            {
                resultKeys.Add(entry.key);
            }
        }

        if (resultKeys.Count == 0)
        {
            Debug.LogWarning(
                $"[TextManager] 검색 결과 없음: Mission={missionId}, Speaker={speakerName}, TextType={textType}"
            );
        }

        return resultKeys;
    }
    private void BuildUITextCache() // UI 캐시 빌드
    {
        _uiTextLookup = new Dictionary<string, string>();

        if (uiTextTable == null)
            return;

        foreach (var e in uiTextTable.entries)
        {
            if (string.IsNullOrEmpty(e.id)) continue;
            _uiTextLookup[e.id] = e.text;
        }
    }
    private void BuildPromptTextCache() // Prompt 캐시 빌드
    {
        _promptTextLookup = new Dictionary<string, string>();

        if (promptTextTable == null)
        {
            Debug.LogWarning("[TextManager] PromptTextTableSO 없음");
            return;
        }

        foreach (var e in promptTextTable.entries)
        {
            if (string.IsNullOrEmpty(e.id))
                continue;

            _promptTextLookup[e.id] = e.text;
        }
    }
    public string GetUIText(string id) // UI전용 API
    {
        if (_uiTextLookup != null &&
            _uiTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[UIText] Not Found: {id}");
        return id;
    }
    public string GetMissionText(int missionNo, MissionTextRole role) // Mission전용 API
    {
        if (missionTextTable == null)
            return role.ToString();

        var set = missionTextTable.missionTextSets
            .Find(s => s.missionIndex == missionNo);

        if (set == null)
            return role.ToString();

        var entry = set.texts
            .Find(t => t.role == role);

        return entry != null ? entry.text : role.ToString();
    }
    public string GetPromptText(string id) // Prompt전용 API
    {
        if (string.IsNullOrEmpty(id))
            return string.Empty;

        if (_promptTextLookup != null &&
            _promptTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[PromptText] Not Found: {id}");
        return string.Empty;
    }

    /// <summary>
    /// 전체 TextEntry 열거 (디버그 / 툴용)
    /// </summary>
    public IEnumerable<TextEntry> GetAllEntries()
    {
        return textDictionary.Values;
    }
}
