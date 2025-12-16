#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PrisonerDataBakerWindow : EditorWindow
{
    [Header("Source CSV")]
    private TextAsset csvAsset;

    [Header("Target Database")]
    private PrisonerDatabaseSO database;

    [MenuItem("Tools/GameData/Prisoner Data Baker")]
    public static void Open()
    {
        GetWindow<PrisonerDataBakerWindow>("Prisoner Data Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prisoner CSV → Database Baker", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        csvAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Prisoner CSV",
            csvAsset,
            typeof(TextAsset),
            false
        );

        database = (PrisonerDatabaseSO)EditorGUILayout.ObjectField(
            "Prisoner Database",
            database,
            typeof(PrisonerDatabaseSO),
            false
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Find or Create Database"))
        {
            database = FindOrCreateDatabase();
        }

        EditorGUILayout.Space();

        GUI.enabled = csvAsset != null && database != null;
        if (GUILayout.Button("Bake CSV into Database"))
        {
            Bake();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        DrawHelpBox();
    }

    private void Bake()
    {
        try
        {
            var lines = csvAsset.text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            if (lines.Count < 2)
            {
                Debug.LogError("[PrisonerBaker] CSV 내용이 비어 있습니다.");
                return;
            }

            database.prisoners.Clear();

            for (int i = 1; i < lines.Count; i++)
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 9)
                {
                    Debug.LogWarning($"[PrisonerBaker] {i + 1}행 컬럼 부족");
                    continue;
                }

                var def = new PrisonerDefinition
                {
                    templateId = cols[0].Trim(),
                    displayName = cols[1].Trim(),
                    type = ParseType(cols[2]),
                    hp = ParseInt(cols[3]),
                    atk = ParseInt(cols[4]),
                    spd = ParseInt(cols[5]),
                    isQte = ParseBool(cols[6]),
                    qteId = NormalizeNull(cols[7]),
                    info = cols[8].Trim()
                };

                database.prisoners.Add(def);
            }

            database.RebuildIndex();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PrisonerBaker] Bake 완료: {database.prisoners.Count}명");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private PrisonerDatabaseSO FindOrCreateDatabase()
    {
        var guids = AssetDatabase.FindAssets("t:PrisonerDatabaseSO");
        if (guids.Length > 0)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<PrisonerDatabaseSO>(path);
        }

        var db = ScriptableObject.CreateInstance<PrisonerDatabaseSO>();
        var savePath = "Assets/GameData/PrisonerDatabase.asset";
        AssetDatabase.CreateAsset(db, savePath);
        AssetDatabase.SaveAssets();
        return db;
    }

    private static string NormalizeNull(string s)
    {
        var v = s.Trim();
        return string.Equals(v, "Null", StringComparison.OrdinalIgnoreCase) ? string.Empty : v;
    }

    private static int ParseInt(string s)
        => int.TryParse(s.Trim(), out var v) ? v : 0;

    private static bool ParseBool(string s)
        => string.Equals(s.Trim(), "TRUE", StringComparison.OrdinalIgnoreCase);

    private static PrisonerType ParseType(string s)
    {
        return string.Equals(s.Trim(), "Bad", StringComparison.OrdinalIgnoreCase)
            ? PrisonerType.Bad
            : PrisonerType.Good;
    }

    private void DrawHelpBox()
    {
        EditorGUILayout.HelpBox(
            "사용 방법:\n" +
            "1. Prisoner CSV에 csv(TextAsset) 드래그\n" +
            "2. Database 지정 또는 Find/Create 클릭\n" +
            "3. Bake 버튼 클릭\n\n" +
            "※ CSV 수정 후 다시 Bake 하면 덮어씁니다.",
            MessageType.Info
        );
    }
}
#endif
