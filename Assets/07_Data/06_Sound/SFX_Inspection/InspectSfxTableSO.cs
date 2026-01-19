using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InspectSfxEntry
{
    [Header("Identity")]
    public InspectObjectType objectType;

    [Header("SFX")]
    public AudioClip animationSfx;   // 애니메이션 시작
}

[CreateAssetMenu(menuName = "Audio/Inspect Animation SFX Table")]
public class InspectSfxTableSO : ScriptableObject
{
    [SerializeField] private List<InspectSfxEntry> entries;

    private Dictionary<InspectObjectType, InspectSfxEntry> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<InspectObjectType, InspectSfxEntry>();

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            if (_lookup.ContainsKey(entry.objectType))
            {
                Debug.LogWarning(
                    $"[InspectSfxTable] Duplicate entry: {entry.objectType}", this
                );
                continue;
            }

            _lookup.Add(entry.objectType, entry);
        }
    }

    public AudioClip GetAnimationSfx(InspectObjectType type)
    {
        if (_lookup == null)
            BuildLookup();

        return _lookup.TryGetValue(type, out var entry)
            ? entry.animationSfx
            : null;
    }
}
