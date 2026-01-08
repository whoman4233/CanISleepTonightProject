using UnityEngine;

[CreateAssetMenu(menuName = "HiddenItem/DefinitionSO")]
public class HiddenItemDefinitionSO : HiddenItemStateSO
{
    [Header("Identity")]
    [SerializeField] private string itemId;          // 고유 ID (Knife_A, Trash_01)
    public string ItemId => itemId;

    [Header("Mission")]
    [SerializeField] private string missionTag;      // Weapon, Forbidden, "" (ex:휴지 같은 의미없는 오브젝트는 Tag 비워두기)
    public string MissionTag => missionTag;

    public bool AffectsMission => !string.IsNullOrEmpty(missionTag);
}
