using UnityEngine;

public class WhiteBoardInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Player player)
    {
        EventBus.Publish(new ShowWhiteBoardPopupEvent());
    }
}
