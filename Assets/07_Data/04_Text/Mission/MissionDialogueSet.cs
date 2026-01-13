using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Dialogue/Mission Dialogue Database")]
public class MissionDialogueDatabase : ScriptableObject
{
    [SerializeField]
    private List<MissionDialogueEntry> missions = new();

    // 런타임 조회용 캐시
    private Dictionary<string, DialogueData> _lookup;

    public DialogueData GetDialogueData(string missionKey)
    {
        if (_lookup == null)
            BuildLookup();

        _lookup.TryGetValue(missionKey, out var data);
        return data;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, DialogueData>();
        foreach (var entry in missions)
        {
            if (entry == null || entry.dialogueData == null)
                continue;

            if (_lookup.ContainsKey(entry.missionKey))
            {
                Debug.LogWarning(
                    $"[MissionDialogueDatabase] Duplicate missionKey: {entry.missionKey}",
                    this
                );
                continue;
            }

            _lookup.Add(entry.missionKey, entry.dialogueData);
        }
    }
}

[System.Serializable]
public class MissionDialogueEntry
{
    public string missionKey;      // Mission01, Mission02 ...
    public MissionDayTheme theme;  // 필요 없으면 제거 가능
    public DialogueData dialogueData;
}
