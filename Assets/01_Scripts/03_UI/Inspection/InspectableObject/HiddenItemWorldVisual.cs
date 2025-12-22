using UnityEngine;

public class HiddenItemWorldVisual : MonoBehaviour
{
    [SerializeField] private HiddenItemHolder itemHolder;
    [SerializeField] private HiddenItemStateSO targetItem;
    [SerializeField] private GameObject worldVisual;

    private void Start()
    {
        if (itemHolder == null || targetItem == null)
            return;

        targetItem.OnFoundStateChanged += OnFoundChanged;
        OnFoundChanged(targetItem.IsFound);
    }

    private void OnDestroy()
    {
        if (targetItem != null)
            targetItem.OnFoundStateChanged -= OnFoundChanged;
    }

    private void OnFoundChanged(bool isFound)
    {
        if (worldVisual != null)
            worldVisual.SetActive(!isFound);
    }
}
