using UnityEngine;

public class MissionItemInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private HiddenItemDefinitionSO itemDefinition;
    [SerializeField] private bool playDiscoverySfx = true;

    public void Interact(Player player)
    {
        // ==========================
        // 즉시 소모형 아이템
        // 상태 저장 X
        // ==========================

        if (playDiscoverySfx)
        {
            EventBus.Publish(new HiddenItemFoundEvent
            {
                ItemDefinition = itemDefinition
            });
        }

        if (itemDefinition.AffectsMission)
        {
            DailyMissionManager.Instance?.NotifyItemFound(itemDefinition.MissionTag);
            Debug.Log($"[Action] 아이템 발견 신고함: {itemDefinition.MissionTag}");
        }

        gameObject.SetActive(false);
    }
}
