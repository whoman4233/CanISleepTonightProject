using UnityEngine;

public class TrashCanInspectionView : MonoBehaviour, IInspectionView
{
    [SerializeField] private GameObject knifeInspectionVisual;

    private KnifeStateSO knifeState;

    public void Bind(IInspectable inspectable)
    {
        if (inspectable is Component component)
        {
            var holder = component.GetComponent<HiddenItemHolder>();
            if (holder != null)
                knifeState = holder.GetItem<KnifeStateSO>();
        }

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (knifeInspectionVisual != null && knifeState != null)
            knifeInspectionVisual.SetActive(!knifeState.IsFound);
    }
}
