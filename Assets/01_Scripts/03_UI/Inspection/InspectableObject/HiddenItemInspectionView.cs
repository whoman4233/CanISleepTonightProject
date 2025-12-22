using UnityEngine;

public class HiddenItemInspectionView : MonoBehaviour, IInspectionView
{
    [SerializeField] private HiddenItemStateSO itemDefinition;
    [SerializeField] private GameObject visual;

    private HiddenItemStateSO runtimeItem;

    public void Bind(IInspectable inspectable)
    {
        if (inspectable is not Component comp)
            return;

        var holder = comp.GetComponent<HiddenItemHolder>();
        if (holder == null)
            return;

        runtimeItem = holder.GetRuntimeItem(itemDefinition);
        if (runtimeItem == null)
            return;

        runtimeItem.OnFoundStateChanged += OnFoundChanged;

        // 초기 상태 반영
        OnFoundChanged(runtimeItem.IsFound);
    }

    private void OnDestroy()
    {
        if (runtimeItem != null)
            runtimeItem.OnFoundStateChanged -= OnFoundChanged;
    }

    private void OnFoundChanged(bool isFound)
    {
        if (visual != null)
            visual.SetActive(!isFound);
    }
}
