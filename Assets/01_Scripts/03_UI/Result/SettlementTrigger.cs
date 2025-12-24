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
            EventBus.Publish(new ShowWarningPopupEvent());
            return;
        }

        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Settlement));
    }
}
