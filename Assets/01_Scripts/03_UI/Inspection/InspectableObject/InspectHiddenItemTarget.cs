using UnityEngine;

public class InspectHiddenItemTarget : MonoBehaviour, IInspectTarget
{
    [Header("숨겨진 아이템")]
    [SerializeField] private HiddenItemStateSO itemDefinition;

    [SerializeField] private GameObject itemVisual;

    public void OnInspect(IInspectable inspectable)
    {
        Debug.Log($"[InspectHiddenItemTarget] OnInspect 호출됨 | this={name}");

        if (itemDefinition == null)
        {
            Debug.LogError("[InspectHiddenItemTarget] itemDefinition null");
            return;
        }

        var owner = inspectable as IHiddenItemInteractable;
        if (owner == null)
        {
            Debug.LogError("[InspectHiddenItemTarget] inspectable이 IHiddenItemInteractable 아님");
            return;
        }

        Debug.Log($"[InspectHiddenItemTarget] TryRevealItem 호출");
        owner.TryRevealItem(itemDefinition);

        if (itemVisual != null)
            itemVisual.SetActive(false);
    }

}
