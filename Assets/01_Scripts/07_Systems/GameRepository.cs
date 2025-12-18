using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRepository
{
    private Dictionary<string, string> dialogueData = new Dictionary<string, string>();

    public void LoadStaticData()
    {
        Debug.Log("CSV/SO 데이터를 메모리에 로드했습니다.");
    }

    public string GetDialogue(string key) => dialogueData.ContainsKey(key) ? dialogueData[key] : "Empty";
}
