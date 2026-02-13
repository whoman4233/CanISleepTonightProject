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
    [SerializeField] public Language CurrentLanguage = Language.Korean;

    // 각 카테고리별로 데이터를 관리하기 위한 구조체
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
        if (CurrentLanguage == lang) return;

        CurrentLanguage = lang;
        RefreshAllCaches();

        OnLanguageChanged?.Invoke();
        Debug.Log($"[TextManager] Language changed to: {lang}");
    }

    private void RefreshAllCaches()
    {
        _dialogueLookup.Clear();
        _uiTextLookup.Clear();
        _promptTextLookup.Clear();
        _currentMissionTable = null;

        // 1. Dialogue 캐시 구성
        var dialogueEntry = dialogueTables.Find(x => x.language == CurrentLanguage);
        if (dialogueEntry.data != null)
        {
            foreach (var t in dialogueEntry.data.textList)
            {
                if (t != null && !string.IsNullOrEmpty(t.key))
                    _dialogueLookup[t.key] = t;
            }
        }

        // 2. UI 캐시 구성
        var uiEntry = uiTextTables.Find(x => x.language == CurrentLanguage);
        if (uiEntry.data != null)
        {
            foreach (var e in uiEntry.data.entries)
            {
                if (!string.IsNullOrEmpty(e.id))
                    _uiTextLookup[e.id] = e.text;
            }
        }

        // 3. Prompt 캐시 구성
        var promptEntry = promptTextTables.Find(x => x.language == CurrentLanguage);
        if (promptEntry.data != null)
        {
            foreach (var e in promptEntry.data.entries)
            {
                if (!string.IsNullOrEmpty(e.id))
                    _promptTextLookup[e.id] = e.text;
            }
        }

        // 4. Mission 테이블 설정
        _currentMissionTable = missionTextTables.Find(x => x.language == CurrentLanguage).data;

        Debug.Log($"[TextManager] 캐시 갱신 완료: {CurrentLanguage}");
    }

    // =======================================================================
    // [Public APIs]
    // =======================================================================

    public string GetText(string key)
    {
        if (_dialogueLookup.TryGetValue(key, out var entry))
        {
            return entry.ko; // 현재 활성화된 SO의 텍스트를 그대로 반환
        }
        return key;
    }

    public string GetUIText(string id)
    {
        if (_uiTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[UIText] Not Found: {id} in {CurrentLanguage}");
        return id;
    }

    public string GetPromptText(string id)
    {
        if (string.IsNullOrEmpty(id)) return string.Empty;

        if (_promptTextLookup.TryGetValue(id, out var text))
            return text;

        Debug.LogWarning($"[PromptText] Not Found: {id} in {CurrentLanguage}");
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
}