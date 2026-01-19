using UnityEngine;

public class MissionItemInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private HiddenItemDefinitionSO itemDefinition;

    public void Interact(Player player)
    {
        // ==========================
        // 발견 확정은 상태에서만
        // ==========================
        itemDefinition.OnFound();

        if (itemDefinition.AffectsMission)
        {
            DailyMissionManager.Instance?.NotifyItemFound(itemDefinition.MissionTag);

            Debug.Log($"[Action] 아이템 발견 신고함: {itemDefinition.MissionTag}");
        }

        gameObject.SetActive(false);
    }
}
