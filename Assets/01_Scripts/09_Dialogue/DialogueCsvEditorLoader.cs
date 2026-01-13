#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class DialogueCsvEditorLoader
{
    // DialogueText.csv 검색
    private const string CSV_SEARCH_FILTER = "t:TextAsset DialogueText";

    public static List<TextEntry> LoadAll()
    {
        var list = new List<TextEntry>();

        // 1. CSV 에셋 검색
        string[] guids = AssetDatabase.FindAssets(CSV_SEARCH_FILTER);

        if (guids.Length == 0)
        {
            Debug.LogError("[DialogueCsvEditorLoader] DialogueText.csv not found");
            return list;
        }

        if (guids.Length > 1)
        {
            Debug.LogWarning(
                "[DialogueCsvEditorLoader] Multiple DialogueText.csv found. Using first one."
            );
        }

        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

        if (csvAsset == null)
        {
            Debug.LogError("[DialogueCsvEditorLoader] Failed to load TextAsset");
            return list;
        }

        // 2. CSV 파싱
        string[] lines = csvAsset.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // header skip
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string line = lines[i].Trim();
            string[] cols = line.Split(',');

            // 최소 6컬럼 + comment까지 고려해 7 권장
            if (cols.Length < 6)
            {
                Debug.LogWarning($"[DialogueCsvEditorLoader] Invalid line: {line}");
                continue;
            }

            string language = cols[1].Trim();
            if (language != "KR")
                continue; // 한국어만 사용

            TextEntry entry = new TextEntry
            {
                key = cols[0].Trim(),
                mission = cols[2].Trim(),
                type = cols[3].Trim(),
                speaker = cols[4].Trim(),
                ko = cols[5].Trim(),
                en = string.Empty // 현재 CSV 구조상 없음
            };

            list.Add(entry);
        }

        return list;
    }
}
#endif


