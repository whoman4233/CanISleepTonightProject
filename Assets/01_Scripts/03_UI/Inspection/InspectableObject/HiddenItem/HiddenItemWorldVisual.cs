using UnityEngine;

public class HiddenItemWorldVisual : MonoBehaviour
{
    [SerializeField] private HiddenItemDefinitionSO itemDefinition;
    [SerializeField] private GameObject worldVisual;

    private HiddenItemDefinitionSO runtimeItem;

    private void Start()
    {
        if (itemDefinition == null || worldVisual == null)
            return;

        var holder = GetComponentInParent<HiddenItemHolder>();
        if (holder == null)
        {
            Debug.LogError("[HiddenItemWorldVisual] HiddenItemHolder not found", this);
            return;
        }

        runtimeItem = holder.GetRuntimeItem(itemDefinition);
        if (runtimeItem == null)
        {
            Debug.LogError($"[HiddenItemWorldVisual] Runtime item not found: {itemDefinition.ItemId}", this);
            return;
        }

        runtimeItem.OnFoundStateChanged += OnFoundChanged;
        OnFoundChanged(runtimeItem.IsFound);
    }

    private void OnDestroy()
    {
        if (runtimeItem != null)
            runtimeItem.OnFoundStateChanged -= OnFoundChanged;
    }

    private void OnFoundChanged(bool isFound)
    {
        Debug.Log($"[HiddenItemWorldVisual] OnFoundChanged isFound={isFound}");
        worldVisual.SetActive(!isFound);
    }
}


