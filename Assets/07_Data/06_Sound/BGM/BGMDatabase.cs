using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Audio/BGM Database")]
public class BGMDatabase : ScriptableObject
{
    [SerializeField] private List<BGMData> bgms;

    private Dictionary<string, BGMData> _lookup;

    private void Build()
    {
        _lookup = new Dictionary<string, BGMData>();
        foreach (var bgm in bgms)
        {
            if (bgm == null || string.IsNullOrEmpty(bgm.sceneName))
                continue;

            if (_lookup.ContainsKey(bgm.sceneName))
            {
                continue;
            }

            _lookup.Add(bgm.sceneName, bgm);
        }
    }

    public BGMData GetByScene(string sceneName)
    {
        if (_lookup == null)
            Build();

        _lookup.TryGetValue(sceneName, out var data);
        return data;
    }
}

