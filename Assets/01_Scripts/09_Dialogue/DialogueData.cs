using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private DialogueLine[] lines;

    public DialogueLine[] Lines => lines;

    public void GenerateRange(int start, int end)
    {
        int count = end - start + 1;
        if (count <= 0) return;

        lines = new DialogueLine[count];
        for (int i = 0; i < count; i++)
        {
            int currentNum = start + i;
            string key = $"Txt_KR_T_{currentNum:D2}";

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


