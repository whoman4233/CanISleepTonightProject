using System.Collections.Generic;
using UnityEngine;
using System;

public class HiddenItemHolder : MonoBehaviour, IHiddenItemInteractable
{
    [SerializeField] private HiddenItemStateSO[] hiddenItems;

    private Dictionary<Type, HiddenItemStateSO> runtimeItems;

    private void Awake()
    {
        runtimeItems = new Dictionary<Type, HiddenItemStateSO>();

        foreach (var item in hiddenItems)
        {
            var instance = Instantiate(item);
            instance.ResetState();

            runtimeItems.Add(item.GetType(), instance);
        }
    }

    public void TryRevealItem(HiddenItemStateSO itemDefinition)
    {
        if (itemDefinition == null)
            return;

        var type = itemDefinition.GetType();

        if (!runtimeItems.TryGetValue(type, out var runtimeItem))
            return;

        if (runtimeItem.IsFound)
            return;

        runtimeItem.OnFound();
    }

    public HiddenItemStateSO GetRuntimeItem(HiddenItemStateSO definition)
    {
        if (definition == null)
            return null;

        runtimeItems.TryGetValue(definition.GetType(), out var item);
        return item;
    }

}




