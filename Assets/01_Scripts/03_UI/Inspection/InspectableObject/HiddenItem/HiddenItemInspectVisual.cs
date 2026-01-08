using UnityEngine;

public class HiddenItemInspectVisual : MonoBehaviour
{
    [SerializeField] private HiddenItemDefinitionSO itemDefinition;
    [SerializeField] private GameObject inspectVisual; // AV에서 숨길 메쉬 루트

    private HiddenItemDefinitionSO _runtimeItem;

    public void BindFromWorld(InspectableObject worldInspectable)
    {
        if (worldInspectable == null || itemDefinition == null || inspectVisual == null)
            return;

        var holder = worldInspectable.GetHiddenItemHolder();
        if (holder == null)
        {
            Debug.LogError("[HiddenItemInspectVisual] HiddenItemHolder not found", this);
            return;
        }

        _runtimeItem = holder.GetRuntimeItem(itemDefinition);
        if (_runtimeItem == null)
        {
            Debug.LogError($"[HiddenItemInspectVisual] Runtime item not found: {itemDefinition.ItemId}", this);
            return;
        }

        _runtimeItem.OnFoundStateChanged += OnFoundChanged;
        OnFoundChanged(_runtimeItem.IsFound);
    }

    private void OnDestroy()
    {
        if (_runtimeItem != null)
            _runtimeItem.OnFoundStateChanged -= OnFoundChanged;
    }

    private void OnFoundChanged(bool isFound)
    {
        inspectVisual.SetActive(!isFound);
    }
}
