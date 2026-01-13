using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private DialogueLine[] lines;

    [Header("미션브리핑대사")]
    public DialogueLine[] briefing;

    [Header("결과 - 공통 대사")]
    public DialogueLine[] fin;

    [Header("결과 - 성공 대사")]
    public DialogueLine[] success;

    [Header("결과 - 실패 대사")]
    public DialogueLine[] fail;

    public DialogueLine[] Lines => lines;

#if UNITY_EDITOR
    [Header("Editor Only")]
    [SerializeField] private string missionKey;
    [SerializeField] private string speakerFilter;
#endif

    public void GenerateRange(int start, int end)
    {
        int count = end - start + 1;
        if (count <= 0) return;

        lines = new DialogueLine[count];
        for (int i = 0; i < count; i++)
        {
            int currentNum = start + i;
            string key = $"DTxt_KR_T_{currentNum:D2}";

            DialogueLine newLine = new DialogueLine();
            newLine.textKey = key;
            lines[i] = newLine;
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets(); // 즉시 저장
#endif
        Debug.Log($"{name}: {start} ~ {end} 범위 생성 완료.");
    }
#if UNITY_EDITOR
    private void GenerateFromCSV()
    {
        var entries = DialogueCsvEditorLoader.LoadAll();

        var briefingList = new List<DialogueLine>();
        var finList = new List<DialogueLine>();
        var successList = new List<DialogueLine>();
        var failList = new List<DialogueLine>();

        foreach (var entry in entries)
        {
            if (entry.mission != missionKey)
                continue;

            if (!string.IsNullOrEmpty(speakerFilter) &&
           entry.speaker != speakerFilter)
                continue;

            DialogueLine line = new DialogueLine { textKey = entry.key };

            switch (entry.type)
            {
                case DialogueKeys.Types.Dialogue:
                    briefingList.Add(line);
                    break;

                case DialogueKeys.Types.Fin:
                    finList.Add(line);
                    break;

                case DialogueKeys.Types.Complete:
                    successList.Add(line);
                    break;

                case DialogueKeys.Types.Fail:
                    failList.Add(line);
                    break;
            }
        }

        briefing = briefingList.ToArray();
        fin = finList.ToArray();
        success = successList.ToArray();
        fail = failList.ToArray();

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[DialogueData] Generated for {missionKey} " +
            $"(Briefing:{briefing.Length}, Fin:{fin.Length}, " +
            $"Success:{success.Length}, Fail:{fail.Length})"
        );
    }

    [ContextMenu("Generate From CSV")]
    private void Context_GenerateFromCSV()
    {
        GenerateFromCSV();
    }
#endif


    // 인스펙터 우클릭 메뉴 (사용 편의성)
    [ContextMenu("Generate 01-16 (Basic)")]
    private void Gen1() => GenerateRange(1, 16);

    [ContextMenu("Generate 17-18 (Box)")]
    private void Gen2() => GenerateRange(17, 18);

    [ContextMenu("Generate 19-20 (Baton)")]
    private void Gen3() => GenerateRange(19, 20);

    [ContextMenu("Generate 21-26 (Hit)")]
    private void Gen4() => GenerateRange(21, 26);

    [ContextMenu("Generate 27-37 (Book)")]
    private void Gen5() => GenerateRange(27, 37);

}


