using UnityEngine;

public class MissionItemInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private HiddenItemDefinitionSO definition;

    public void Interact(Player player)
    {
        if (definition.AffectsMission)
        {
            DailyMissionManager.Instance
                ?.NotifyItemFound(definition.MissionTag);
        }

        gameObject.SetActive(false);
    }
}
