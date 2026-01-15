#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;

public class UITextCsvBakerWindow : EditorWindow
{
    private TextAsset csvAsset;
    private UITextTableSO table;

    [MenuItem("Tools/GameData/UI Text Baker")]
    public static void Open()
    {
        GetWindow<UITextCsvBakerWindow>("UI Text Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI CSV → UITextTableSO", EditorStyles.boldLabel);

        csvAsset = (TextAsset)EditorGUILayout.ObjectField(
            "UI CSV", csvAsset, typeof(TextAsset), false
        );
        table = (UITextTableSO)EditorGUILayout.ObjectField(
            "Target SO", table, typeof(UITextTableSO), false
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
        if (table == null)
        {
            Debug.LogError("[UITextCsvBaker] Target SO is null");
            return;
        }

        if (table.entries == null)
        {
            table.entries = new List<UITextEntry>();
        }

        var lines = csvAsset.text.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );

        Undo.RecordObject(table, "Bake UI Text");
        table.entries.Clear();

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 2) continue;

            table.entries.Add(new UITextEntry
            {
                id = cols[0].Trim(),
                text = cols[1].Trim(),
                info = cols.Length > 2 ? cols[2].Trim() : string.Empty
            });
        }

        EditorUtility.SetDirty(table);
        AssetDatabase.SaveAssets();
    }

    private UITextTableSO FindOrCreate()
    {
        var guids = AssetDatabase.FindAssets("t:UITextTableSO");
        if (guids.Length > 0)
            return AssetDatabase.LoadAssetAtPath<UITextTableSO>(
                AssetDatabase.GUIDToAssetPath(guids[0])
            );

        var so = CreateInstance<UITextTableSO>();
        AssetDatabase.CreateAsset(so, "Assets/GameData/UITextTable.asset");
        AssetDatabase.SaveAssets();
        return so;
    }
}
#endif
