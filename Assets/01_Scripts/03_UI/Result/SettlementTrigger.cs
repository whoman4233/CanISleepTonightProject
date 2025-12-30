using UnityEngine;

public class SettlementTrigger : MonoBehaviour, IInteractable
{
    public bool CanEnterSettlement()
    {
        return GameManager.Instance.CurrentPhase == GamePhase.Patrol;
    }

    public void Interact(Player player)
    {
        if (!CanEnterSettlement())
        {
            EventBus.Publish(new ShowTimedTextPopupEvent("순찰하지 않으면 보고 할 수 없어", 1f));
            return;
        }

        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Settlement));
    }
}
