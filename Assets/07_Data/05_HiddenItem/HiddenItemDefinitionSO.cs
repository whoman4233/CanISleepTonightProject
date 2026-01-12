using UnityEngine;

[CreateAssetMenu(menuName = "HiddenItem/DefinitionSO")]
public class HiddenItemDefinitionSO : HiddenItemStateSO
{
    [Header("Identity")]
    [SerializeField] private string itemId;
    public string ItemId => itemId;

    [Header("Mission")]
    [SerializeField] private string missionTag;
    public string MissionTag => missionTag;
    public bool AffectsMission => !string.IsNullOrEmpty(missionTag);

    [Header("Discovery")]
    public bool requiresInspection;   // Inspection 전용인가?
}
