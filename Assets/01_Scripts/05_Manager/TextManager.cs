using System;
using System.Collections.Generic;
using UnityEngine;

public class TextManager : MonoBehaviour
{
    public static TextManager Instance;

    [Header("데이터 참조")]
    [SerializeField] private TextSOData textData; // SO 연결

    [Header("설정")]
    [SerializeField] private Language currentLanguage = Language.Korean;

    // 핵심: 런타임 조회용 딕셔너리 (Key -> 현재 언어 텍스트)
    private Dictionary<string, TextEntry> textDictionary = new Dictionary<string, TextEntry>();

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
            InitializeDictionary(); // 최초 초기화

            OnTextDataReady?.Invoke(); // 텍스트 시스템 준비 완료 알림(UI 먼저 생성되도 이 시점에 갱신)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 딕셔너리 구축 (언어 바뀔 때마다 호출)
    public void SetLanguage(Language lang)
    {
        // 같은 언어 재설정 방지
        if (currentLanguage == lang)
            return;

        currentLanguage = lang;
        InitializeDictionary();

        // 언어 변경 알림
        OnLanguageChanged?.Invoke();
        // 여기에 언어 변경 이벤트(Event)를 발생시켜 UI들이 갱신되게 하면 더 좋습니다.
        Debug.Log($"Language changed to: {lang}");
    }

    private void InitializeDictionary()
    {
        if (textData == null)
        {
            Debug.LogError("TextSOData가 연결되지 않았습니다!");
            return;
        }

        textDictionary.Clear();

        //foreach (var entry in textData.textList)
        //{
        //    // 중복 키 방지 체크
        //    if (textDictionary.ContainsKey(entry.key))
        //    {
        //        Debug.LogWarning($"중복된 키가 있습니다: {entry.key}");
        //        continue;
        //    }

        //    // 현재 언어 설정에 따라 밸류 결정
        //    string value = (currentLanguage == Language.Korean) ? entry.ko : entry.en;
        //    textDictionary.Add(entry.key, value);
        //}
        foreach (var entry in textData.textList)
        {
            if (!textDictionary.ContainsKey(entry.key))
            {
                // value에 entry(객체 전체)를 넣음
                textDictionary.Add(entry.key, entry);
            }
        }
    }

    // 외부에서 텍스트 가져오는 함수
    //public string GetText(string key)
    //{
    //    //if (textDictionary.TryGetValue(key, out string value))
    //    //{
    //    //    return value;
    //    //}

    //    //Debug.LogError($"텍스트 키를 찾을 수 없음: {key}");
    //    //return key; // 에러 시 키값이라도 반환해서 UI가 비지 않게 함
    //}

    public TextEntry GetEntry(string key)
    {
        if (textDictionary.TryGetValue(key, out var entry))
        {
            return entry;
        }

        Debug.LogError($"[TextManager] 키를 찾을 수 없음: {key}");
        return null;
    }
    public string GetText(string key)
    {
        var entry = GetEntry(key);
        if (entry == null) return key;

        return currentLanguage == Language.Korean ? entry.ko : entry.en;
    }
}