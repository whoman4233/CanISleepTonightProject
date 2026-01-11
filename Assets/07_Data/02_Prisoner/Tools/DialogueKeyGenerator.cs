#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DialogueKeyGenerator
{
    // 유니티 상단 메뉴에 버튼 생성
    [MenuItem("Tools/Generate Dialogue Keys")]
    public static void Generate()
    {
        // 데이터 소스 가져오기
        string assetPath = "Assets/07_Data/04_Text/DialogueTextData.asset"; // 실제 경로에 맞게 수정
        TextSOData data = AssetDatabase.LoadAssetAtPath<TextSOData>(assetPath);

        if (data == null)
        {
            Debug.LogError("TextSOData를 찾을 수 없습니다! 경로를 확인하세요.");
            return;
        }

        // 중복 없는 데이터 추출
        var missions = data.textList.Select(x => x.mission).Distinct().Where(x => !string.IsNullOrEmpty(x));
        var speakers = data.textList.Select(x => x.speaker).Distinct().Where(x => !string.IsNullOrEmpty(x));
        var types = data.textList.Select(x => x.type).Distinct().Where(x => !string.IsNullOrEmpty(x));

        // 코드 문자열 생성
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 이 파일은 자동 생성되었습니다. 수동 수정 금지!");
        sb.AppendLine("public static class DialogueKeys");
        sb.AppendLine("{");

        GenerateInnerClass(sb, "Missions", missions);
        GenerateInnerClass(sb, "Speakers", speakers);
        GenerateInnerClass(sb, "Types", types);

        // 인스펙터용 이넘 생성
        GenerateEnum(sb, "MissionType", missions);
        GenerateEnum(sb, "SpeakerType", speakers);
        GenerateEnum(sb, "DialogueType", types);

        sb.AppendLine("}");

        // 4. 파일 쓰기
        string outputPath = Path.Combine(Application.dataPath, "07_Data/04_Text/DialogueKeys.cs");
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        
        AssetDatabase.Refresh(); // 유니티가 새 파일을 인식하도록 갱신
        Debug.Log("<color=green>DialogueKeys 자동 생성 완료!</color>");
    }

    private static void GenerateInnerClass(StringBuilder sb, string className, IEnumerable<string> items)
    {
        sb.AppendLine($"    public static class {className}");
        sb.AppendLine("    {");
        foreach (var item in items)
        {
            // 변수 이름으로 쓸 수 없는 특수문자 제거
            string varName = item.Replace(" ", "").Replace("-", "_");
            sb.AppendLine($"        public const string {varName} = \"{item}\";");
        }
        sb.AppendLine("    }");
    }
    private static void GenerateEnum(StringBuilder sb, string enumName, IEnumerable<string> items)
    {
        sb.AppendLine($"    public enum {enumName}");
        sb.AppendLine("    {");
        foreach (var item in items)
        {
            string varName = item.Replace(" ", "").Replace("-", "_").Replace("(", "").Replace(")", "");
            sb.AppendLine($"        {varName},");
        }
        sb.AppendLine("    }");
    }
}
#endif