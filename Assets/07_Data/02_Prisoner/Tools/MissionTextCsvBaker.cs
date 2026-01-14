#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MissionTextCsvBakerWindow : EditorWindow
{
    private TextAsset csvAsset;
    private MissionTextTableSO table;

    [MenuItem("Tools/GameData/Mission Text Baker")]
    public static void Open()
    {
        GetWindow<MissionTextCsvBakerWindow>("Mission Text Baker");
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
            var cols = lines[i].Split(',');
            if (cols.Length < 3)
                continue;

            // CSV 구조
            // 0: TextID
            // 1: MissionNO
            // 2: Text
            // 3: Info (optional)

            var id = cols[0].Trim();

            if (!int.TryParse(cols[1].Trim(), out var missionNo))
            {
                Debug.LogError(
                    $"[MissionTextBaker] Invalid MissionNO at line {i + 1}: {cols[1]}"
                );
                continue;
            }

            var text = cols[2].Trim();
            var info = cols.Length > 3 ? cols[3].Trim() : "";

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
                id = id,
                text = text,
                info = info
            });
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
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
