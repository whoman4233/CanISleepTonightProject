using UnityEngine;

public class HiddenItemInspectionView : MonoBehaviour, IInspectionView
{
    [SerializeField] private HiddenItemStateSO targetItem;
    [SerializeField] private GameObject visual;

    public void Bind(IInspectable inspectable)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (visual != null && targetItem != null)
            visual.SetActive(!targetItem.IsFound);
    }
}
