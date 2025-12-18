using UnityEngine;

public class InspectKnifeTarget : MonoBehaviour, IInspectTarget
{
    [SerializeField] private GameObject knifeVisual;

    public void OnInspect(IInspectable inspectable)
    {
        if (inspectable is not Component component)
            return;

        var holder = component.GetComponentInParent<HiddenItemHolder>();
        if (holder == null)
            return;

        var knifeState = holder.GetItem<KnifeStateSO>();
        if (knifeState == null)
            return;

        Debug.Log($"[Inspect] KnifeState ID={knifeState.GetInstanceID()}");

        knifeState.OnFound();

        if (knifeVisual != null)
            knifeVisual.SetActive(false);
    }
}


