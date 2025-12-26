using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectHiddenItemAction : MonoBehaviour, IInspectAction
{
    [SerializeField] private HiddenItemStateSO itemDefinition;

    public void InspectAction(IInspectable owner)
    {
        if (owner is IHiddenItemInteractable interactable)
        {
            interactable.TryRevealItem(itemDefinition);
        }
    }
}
