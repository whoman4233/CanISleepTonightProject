using UnityEngine;

public class HiddenItemWorldVisual : MonoBehaviour
{
    [SerializeField] private HiddenItemStateSO itemDefinition;
    [SerializeField] private GameObject worldVisual;

    private HiddenItemStateSO runtimeItem;

    private void Start()
    {
        var holder = GetComponentInParent<HiddenItemHolder>();
        runtimeItem = holder.GetRuntimeItem(itemDefinition);

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
        worldVisual.SetActive(!isFound);
    }
}

