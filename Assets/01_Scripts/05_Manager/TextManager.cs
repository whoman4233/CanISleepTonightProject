using System;
using System.Collections.Generic;
using UnityEngine;

public enum TextTableType
{
    Dialogue,
    UI,
    Mission,
    Prompt,
    Tutorial
}

public class TextManager : MonoBehaviour
{
    public static TextManager Instance;

    [Header("설정")]
    [SerializeField] private Language currentLanguage = Language.Korean;
    public Language CurrentLanguage => currentLanguage;

    [Serializable]
    public struct TableEntry<T> where T : ScriptableObject
    {
        public Language language;
        public T data;
    }

    [Header("1. Dialogue Text Tables")]
    [SerializeField] private List<TableEntry<TextSOData>> dialogueTables = new List<TableEntry<TextSOData>>();
    private Dictionary<string, TextEntry> _dialogueLookup = new Dictionary<string, TextEntry>();

    [Header("2. UI Text Tables")]
    [SerializeField] private List<TableEntry<UITextTableSO>> uiTextTables = new List<TableEntry<UITextTableSO>>();
    private Dictionary<string, string> _uiTextLookup = new Dictionary<string, string>();

    [Header("3. Mission Text Tables")]
    [SerializeField] private List<TableEntry<MissionTextTableSO>> missionTextTables = new List<TableEntry<MissionTextTableSO>>();
    private MissionTextTableSO _currentMissionTable;

    [Header("4. Prompt Text Tables")]
    [SerializeField] private List<TableEntry<UITextTableSO>> promptTextTables = new List<TableEntry<UITextTableSO>>();
    private Dictionary<string, string> _promptTextLookup = new Dictionary<string, string>();

    [Header("5. Tutorial Text Tables")]
    [SerializeField] private List<TableEntry<UITextTableSO>> tutorialTextTables = new List<TableEntry<UITextTableSO>>();
    private Dictionary<string, string> _tutorialTextLookup = new Dictionary<string, string>();

    public static event Action OnTextDataReady;
    public static event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshAllCaches();
            OnTextDataReady?.Invoke();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(Language lang)
    {
        if (currentLanguage == lang) return;

        currentLanguage = lang;
        RefreshAllCaches();

        OnLanguageChanged?.Invoke();
        Debug.Log($"[TextManager] Language changed to: {lang}");
    }

    private void RefreshAllCaches()
    {
        _dialogueLookup.Clear();
        _uiTextLookup.Clear();
        _promptTextLookup.Clear();
        _tutorialTextLookup.Clear();
        _currentMissionTable = null;

        // 1. Dialogue 캐시 구성
        var dialogueEntry = dialogueTables.Find(x => x.language == currentLanguage);
        if (dialogueEntry.data != null)
        {
            foreach (var t in dialogueEntry.data.textList)
            {
                if (t != null && !string.IsNullOrEmpty(t.key))
                    _dialogueLookup[t.key] = t;
            }
        }

        // 2. UI 캐시 구성
        var uiEntry = uiTextTables.Find(x => x.language == currentLanguage);
        if (uiEntry.data != null)
        {
            foreach (var e in uiEntry.data.entries)
            {
                if (!string.IsNullOrEmpty(e.id))
                    _uiTextLookup[e.id] = e.text;
            }
        }

        // 3. Prompt 캐시 구성
        var promptEntry = promptTextTables.Find(x => x.language == currentLanguage);
        if (promptEntry.data != null)
        {
            foreach (var e in promptEntry.data.entries)
            {
                if (!string.IsNullOrEmpty(e.id))
                    _promptTextLookup[e.id] = e.text;
            }
        }

        // 4. Tutorial 캐시 구성
        var tutorialEntry = tutorialTextTables.Find(x => x.language == currentLanguage);
        if (tutorialEntry.data != null)
        {
            foreach (var e in tutorialEntry.data.entries)
            {
                if (!string.IsNullOrEmpty(e.id))
                    _tutorialTextLookup[e.id] = e.text;
            }
        }

        // 5. Mission 테이블 설정
        var missionEntry = missionTextTables.Find(x => x.language == currentLanguage);
        _currentMissionTable = missionEntry.data;

        Debug.Log($"[TextManager] 캐시 갱신 완료: {currentLanguage} | UI 캐시 수: {_uiTextLookup.Count}");
    }

    // =======================================================================
    // [Public APIs]
    // =======================================================================

    public string GetText(string key)
    {
        if (_dialogueLookup.TryGetValue(key, out var entry))
        {
            // [중요 수정] 이미 언어별 SO를 캐싱했으므로, 
            // 데이터가 입력된 필드를 우선적으로 반환하도록 로직 보강.
            // 만약 영어 SO에서 데이터를 'en' 필드에 넣었다면 아래 조건이 정상 작동합니다.
            if (currentLanguage == Language.Korean) return entry.ko;
            return string.IsNullOrEmpty(entry.en) ? entry.ko : entry.en;
        }
        return key;
    }

    public string GetUIText(string id)
    {
        if (_uiTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[UIText] Not Found: {id} in {currentLanguage}");
        return id;
    }

    public string GetPromptText(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;

        if (_promptTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[PromptText] Not Found: {id} in {currentLanguage}");
        return id;
    }

    public string GetTutorialText(string id)
    {
        if (_tutorialTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[Tutorial] Key not found: {id} in {currentLanguage}");
        return id;
    }

    public string GetMissionText(int missionNo, MissionTextRole role)
    {
        if (_currentMissionTable == null) return role.ToString();

        var set = _currentMissionTable.missionTextSets.Find(s => s.missionIndex == missionNo);
        if (set == null) return role.ToString();

        var entry = set.texts.Find(t => t.role == role);
        return entry != null ? entry.text : role.ToString();
    }

    public List<string> GetKeysByMissionAndSpeaker(string missionId, string speakerName, string textType)
    {
        List<string> resultKeys = new List<string>();
        foreach (var entry in _dialogueLookup.Values)
        {
            if (entry.mission == missionId && entry.speaker == speakerName && entry.type == textType)
                resultKeys.Add(entry.key);
        }
        return resultKeys;
    }

    public TextEntry GetEntry(string key)
    {
        _dialogueLookup.TryGetValue(key, out var entry);
        return entry;
    }
}