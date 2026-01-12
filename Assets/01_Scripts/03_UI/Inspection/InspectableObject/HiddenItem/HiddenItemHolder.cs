using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HiddenItemEntry
{
    public HiddenItemDefinitionSO definition;
    public HiddenItemStateSO state;
}
public class HiddenItemHolder : MonoBehaviour, IHiddenItemInteractable
{
    [SerializeField] private HiddenItemDefinitionSO[] hiddenItems;

    // itemId → runtime instance
    private Dictionary<string, HiddenItemDefinitionSO> runtimeItems;

    private void Awake()
    {
        runtimeItems = new Dictionary<string, HiddenItemDefinitionSO>();

        foreach (var def in hiddenItems)
        {
            if (def == null || string.IsNullOrEmpty(def.ItemId))
            {
                Debug.LogError("[HiddenItemHolder] Invalid HiddenItemDefinitionSO", this);
                continue;
            }

            if (runtimeItems.ContainsKey(def.ItemId))
            {
                Debug.LogError($"[HiddenItemHolder] Duplicate ItemId: {def.ItemId}", this);
                continue;
            }

            var instance = Instantiate(def); // 런타임 상태 분리
            instance.ResetState();

            runtimeItems.Add(def.ItemId, instance);
        }
    }

    public void TryRevealItem(HiddenItemStateSO itemDefinition)
    {
        Debug.Log($"[HiddenItemHolder] TryRevealItem called: {itemDefinition?.name}");
        if (itemDefinition is not HiddenItemDefinitionSO def)
            return;

        if (!runtimeItems.TryGetValue(def.ItemId, out var runtimeItem))
            return;

        if (runtimeItem.IsFound)
            return;
        Debug.Log($"[HiddenItemHolder] Found -> OnFound() call, runtime IsFound(before)={runtimeItem.IsFound}");
        runtimeItem.OnFound();
    }

    public HiddenItemDefinitionSO GetRuntimeItem(HiddenItemDefinitionSO definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.ItemId))
            return null;

        runtimeItems.TryGetValue(definition.ItemId, out var item);
        return item;
    }
}





