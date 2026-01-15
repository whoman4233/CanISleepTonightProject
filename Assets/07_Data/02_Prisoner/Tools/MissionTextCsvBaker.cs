#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Text.RegularExpressions;

public class MissionTextCsvBaker : EditorWindow
{
    private TextAsset csvAsset;
    private MissionTextTableSO table;

    // 쉼표가 포함된 문자열을 안전하게 파싱하기 위한 CSV 정규식
    private static readonly Regex CsvParser =
        new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    [MenuItem("Tools/GameData/Mission Text Baker")]
    public static void Open()
    {
        GetWindow<MissionTextCsvBaker>("Mission Text Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Mission CSV → MissionTextTableSO", EditorStyles.boldLabel);

        csvAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Mission CSV", csvAsset, typeof(TextAsset), false
        );

        table = (MissionTextTableSO)EditorGUILayout.ObjectField(
            "Target SO", table, typeof(MissionTextTableSO), false
        );

        if (GUILayout.Button("Find or Create Table"))
            table = FindOrCreate();

        GUI.enabled = csvAsset != null && table != null;
        if (GUILayout.Button("Bake"))
            Bake();
        GUI.enabled = true;
    }

    private void Bake()
    {
        var lines = csvAsset.text.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );

        Undo.RecordObject(table, "Bake Mission Text");

        if (table.missionTextSets == null)
            table.missionTextSets = new List<MissionTextSet>();
        else
            table.missionTextSets.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            // 정규식 기반 CSV 파싱
            var cols = CsvParser.Split(lines[i]);
            if (cols.Length < 3)
                continue;

            // CSV 구조
            // 0: Role
            // 1: MissionNo
            // 2: Text
            // 3: Info (optional)

            // Role 문자열 → Enum 변환
            var roleStr = cols[0].Trim();
            if (!Enum.TryParse<MissionTextRole>(roleStr, true, out var role))
            {
                Debug.LogError(
                    $"[MissionTextBaker] Invalid Role at line {i + 1}: {roleStr}"
                );
                continue;
            }

            // MissionNo 파싱
            var missionNoStr = cols[1].Trim();
            if (!int.TryParse(missionNoStr, out var missionNo))
            {
                Debug.LogError(
                    $"[MissionTextBaker] Invalid MissionNO at line {i + 1}: {missionNoStr}"
                );
                continue;
            }

            // 텍스트 처리
            var text = cols[2]
                .Trim()
                .Trim('"')
                .Replace("\"\"", "\"")
                .Replace("<br>", "\n"); // 핵심: 줄바꿈 토큰 처리

            // Info (Editor 전용)
            var info = cols.Length > 3
                ? cols[3]
                    .Trim()
                    .Trim('"')
                    .Replace("\"\"", "\"")
                : "";

            var set = table.missionTextSets
                .Find(s => s.missionIndex == missionNo);

            if (set == null)
            {
                set = new MissionTextSet
                {
                    missionIndex = missionNo,
                    texts = new List<MissionTextEntry>()
                };
                table.missionTextSets.Add(set);
            }

            set.texts.Add(new MissionTextEntry
            {
                role = role,
                text = text,
#if UNITY_EDITOR
                info = info
#endif
            });
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();

        Debug.Log("[MissionTextBaker] Bake 완료");
    }

    private MissionTextTableSO FindOrCreate()
    {
        var guids = AssetDatabase.FindAssets("t:MissionTextTableSO");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<MissionTextTableSO>(
                AssetDatabase.GUIDToAssetPath(guids[0])
            );

        var so = CreateInstance<MissionTextTableSO>();
        AssetDatabase.CreateAsset(so, "Assets/GameData/MissionTextTable.asset");
        AssetDatabase.SaveAssets();
        return so;
    }
}
#endif

