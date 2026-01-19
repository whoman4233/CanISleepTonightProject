using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Audio/BGM Database")]
public class BGMDatabase : ScriptableObject
{
    [SerializeField] private List<BGMData> bgmList;

    private Dictionary<GamePhase, BGMData> _lookup;

    // 모든 BGM 열거용 (읽기 전용) *비동기 로딩을 통한 렉유발 해결
    public IReadOnlyList<BGMData> All => bgmList;

    public BGMData Get(GamePhase phase)
    {
        if (_lookup == null)
            BuildLookup();

        _lookup.TryGetValue(phase, out var data);
        return data;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<GamePhase, BGMData>();

        foreach (var bgm in bgmList)
        {
            if (bgm == null)
                continue;

            if (_lookup.ContainsKey(bgm.phase))
            {
                Debug.LogWarning(
                    $"[BGMDatabase] Duplicate BGM for phase: {bgm.phase}",
                    this
                );
                continue;
            }

            _lookup.Add(bgm.phase, bgm);
        }
    }
}
