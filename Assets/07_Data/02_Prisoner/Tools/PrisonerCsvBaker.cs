#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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

        csvAsset = (TextAsset)EditorGUILayout.ObjectField("Prisoner CSV", csvAsset, typeof(TextAsset), false);
        database = (PrisonerDatabaseSO)EditorGUILayout.ObjectField("Prisoner Database", database, typeof(PrisonerDatabaseSO), false);

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

            // Undo 등록 (실수했을 때 Ctrl+Z 가능하게)
            Undo.RecordObject(database, "Bake Prisoner Data");
            database.prisoners.Clear();

            // 헤더 건너뛰고 1번 인덱스부터 시작
            for (int i = 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue; // 빈 줄 무시

                var cols = line.Split(',');

                // CSV 구조:
                // 0:ID, 1:Name, 2:Type, 3:HP, 4:ATK, 5:Spd, 6:Info, 7:Empty, 8:Empty
                // 최소 7개 컬럼(Info까지)은 있어야 함
                if (cols.Length < 7)
                {
                    Debug.LogWarning($"[PrisonerBaker] {i + 1}행 데이터 부족 (컬럼 수: {cols.Length}) -> Skip");
                    continue;
                }

                var def = new PrisonerDefinition
                {
                    templateId = cols[0].Trim(),
                    displayName = cols[1].Trim(),

                    // [변경] Enum 파싱 (Skinny, Gang 등 자동 매핑)
                    traitType = ParseEnum<PrisonerType>(cols[2]),

                    hp = ParseInt(cols[3]),
                    atk = ParseInt(cols[4]),
                    spd = ParseInt(cols[5]),

                    // [변경] Info 위치 수정 (6번 인덱스)
                    info = cols[6].Trim()

                    // QTE 관련 컬럼은 CSV에 없으므로 기본값 사용 (필요시 추가)
                    // isQte = false,
                    // qteId = ""
                };

                database.prisoners.Add(def);
            }

            database.RebuildIndex(); // 딕셔너리 등 내부 캐시 갱신
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=cyan>[PrisonerBaker]</color> Bake 완료! 총 {database.prisoners.Count}명의 죄수 데이터가 갱신되었습니다.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[PrisonerBaker] 에러 발생: {e.Message}");
            Debug.LogException(e);
        }
    }

    // -----------------------------------------------------------------------
    // Helper Methods
    // -----------------------------------------------------------------------

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

        // 폴더 없으면 생성
        if (!AssetDatabase.IsValidFolder("Assets/GameData"))
            AssetDatabase.CreateFolder("Assets", "GameData");

        AssetDatabase.CreateAsset(db, savePath);
        AssetDatabase.SaveAssets();
        return db;
    }

    private static int ParseInt(string s) => int.TryParse(s.Trim(), out var v) ? v : 0;

    // [신규] 제네릭 Enum 파서 (Skinny, Gang 등을 자동으로 PrisonerType으로 변환)
    private static T ParseEnum<T>(string s) where T : struct, Enum
    {
        var strVal = s.Trim();
        if (Enum.TryParse<T>(strVal, true, out var result))
        {
            return result;
        }

        Debug.LogWarning($"[PrisonerBaker] 알 수 없는 타입: '{strVal}'. 기본값으로 설정합니다.");
        return default(T);
    }

    private void DrawHelpBox()
    {
        EditorGUILayout.HelpBox(
            "CSV 형식 가이드:\n" +
            "ID, Name, Type(Skinny/Gang...), HP, ATK, Spd, Info\n\n" +
            "※ Type은 코드상의 PrisonerType Enum 이름과 정확히 일치해야 합니다.",
            MessageType.Info
        );
    }
}
#endif