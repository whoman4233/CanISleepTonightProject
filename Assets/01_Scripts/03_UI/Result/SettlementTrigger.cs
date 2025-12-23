using UnityEngine;

public class SettlementTrigger : MonoBehaviour, IInteractable
{
    public bool CanInteract()
    {
        return GameManager.Instance.CurrentPhase == GamePhase.Patrol;
    }

    public void Interact(Player player)
    {
        if (!CanInteract())
        {
            EventBus.Publish(new ShowWarningPopupEvent());
            return;
        }

        EventBus.Publish(new RequestPhaseChangeEvent(GamePhase.Settlement));
    }
}
