using System;
using System.Collections.Generic;
using UnityEngine;

public enum TextTableType
{
    Dialogue,
    UI,
    Mission,
    Prompt
}

public class TextManager : MonoBehaviour
{
    public static TextManager Instance;

    [Header("설정")]
    // [수정] 외부에서 읽기 편하게 프로퍼티로 노출하거나, 변수명을 통일합니다.
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
        var tutorialEntry = tutorialTextTables.Find(x => x.language == currentLanguage);
        if (tutorialEntry.data != null)
        {
            foreach (var e in tutorialEntry.data.entries)
            {
                if (!string.IsNullOrEmpty(e.id))
                    _tutorialTextLookup[e.id] = e.text;
            }
        }

        // 4. Mission 테이블 설정 (Find 결과가 없을 경우 대비 null 조건부 연산자 사용)
        _currentMissionTable = missionTextTables.Find(x => x.language == currentLanguage).data;

        Debug.Log($"[TextManager] 캐시 갱신 완료: {currentLanguage}");
    }

    // =======================================================================
    // [Public APIs]
    // =======================================================================

    public string GetText(string key)
    {
        if (_dialogueLookup.TryGetValue(key, out var entry))
        {
            // SO를 언어별로 쪼개서 관리하므로, 해당 SO에 들어있는 텍스트를 반환해야 합니다.
            // 보통 언어별 SO를 만들 때 해당 언어 데이터를 ko 또는 en 필드 중 하나에 몰아넣으므로 
            // 현재 언어에 맞는 필드를 참조하도록 수정합니다.
            return currentLanguage == Language.Korean ? entry.ko : entry.en;
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
            {
                resultKeys.Add(entry.key);
            }
        }
        return resultKeys;
    }

    public TextEntry GetEntry(string key)
    {
        _dialogueLookup.TryGetValue(key, out var entry);
        return entry;
    }

    public string GetTutorialText(string id)
    {
        if (_tutorialTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[Tutorial] Key not found: {id}");
        return id;
    }
}