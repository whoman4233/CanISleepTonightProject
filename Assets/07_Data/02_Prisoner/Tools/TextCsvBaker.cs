#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions; // 정규식 필수

public class TextDataBakerWindow : EditorWindow
{
    private TextAsset _csvAsset;
    private TextSOData _database;

    // 현업용 CSV 정규식: 문장 안의 쉼표는 무시하고 구분자 쉼표만 인식
    private static readonly Regex _csvParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    [MenuItem("Tools/GameData/Text Data Baker")]
    public static void Open() => GetWindow<TextDataBakerWindow>("Text Data Baker");

    private void OnGUI()
    {
        GUILayout.Label("Text CSV → Text Database Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _csvAsset = (TextAsset)EditorGUILayout.ObjectField("Text CSV", _csvAsset, typeof(TextAsset), false);
        _database = (TextSOData)EditorGUILayout.ObjectField("Text Database", _database, typeof(TextSOData), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Find or Create Database"))
            _database = FindOrCreateDatabase();

        EditorGUILayout.Space();

        GUI.enabled = _csvAsset != null && _database != null;
        if (GUILayout.Button("Bake CSV into Database"))
            Bake();
        GUI.enabled = true;
    }

    private void Bake()
    {
        try
        {
            // 줄바꿈 대응 (현업 기준 \r\n, \n 모두 처리)
            var lines = _csvAsset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2) return;

            Undo.RecordObject(_database, "Bake Text Data");
            _database.textList.Clear();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Split(',') 대신 정규식 사용 (데이터 깨짐 방지)
                var cols = _csvParser.Split(line);

                if (cols.Length < 5) continue;

                var entry = new TextEntry
                {
                    // CSV 구조: 0:ID, 1:Lang, 2:미션, 3:타입 4:Speaker, 5:Text
                    key = cols[0].Trim(),
                    mission = cols[2].Trim(),
                    type = cols[3].Trim(),
                    speaker = cols[4].Trim().Trim('"'),
                    ko = cols[5].Trim().Trim('"').Replace("\"\"", "\""), // 따옴표 가공
                    en = "" // 필요시 확장
                };

                _database.textList.Add(entry);
            }

            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=green>[TextBaker]</color> Bake 완료! 총 {_database.textList.Count}개 대사 갱신.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[TextBaker] 에러: {e.Message}");
        }
    }

    private TextSOData FindOrCreateDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:TextSOData");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<TextSOData>(path);
        }

        var db = ScriptableObject.CreateInstance<TextSOData>();
        if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            AssetDatabase.CreateFolder("Assets", "GameData");

        AssetDatabase.CreateAsset(db, "Assets/GameData/TextDatabase.asset");
        AssetDatabase.SaveAssets();
        return db;
    }
}
#endif