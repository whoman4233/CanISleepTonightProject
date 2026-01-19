using System.Collections.Generic;
using UnityEngine;

public enum InteractionState
{
    CanPickUp,
    CanDrop
}

[System.Serializable]
public class InteractionSfxRule
{
    public string objectType;      // Pillow, CellDoor 등
    public InteractionState state; // PickUp / Drop
    public AudioClip clip;
}

[CreateAssetMenu(menuName = "Audio/SFX Rule Table")]
public class InteractionSfxRuleTableSO : ScriptableObject
{
    public List<InteractionSfxRule> rules;

    public AudioClip GetClip(string objectType, InteractionState state)
    {
        foreach (var rule in rules)
        {
            if (rule.objectType == objectType &&
                rule.state == state)
            {
                return rule.clip;
            }
        }
        return null;
    }
}
