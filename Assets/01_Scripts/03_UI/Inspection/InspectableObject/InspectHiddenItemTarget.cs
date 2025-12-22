using UnityEngine;

public class InspectHiddenItemTarget : MonoBehaviour, IInspectTarget
{
    [SerializeField] private HiddenItemHolder itemHolder;
    [SerializeField] private HiddenItemStateSO targetItem;
    [SerializeField] private GameObject itemVisual;

    public void OnInspect(IInspectable inspectable)
    {
        if (targetItem == null)
            return;

        targetItem.OnFound();

        if (itemVisual != null)
            itemVisual.SetActive(false);
    }
}
