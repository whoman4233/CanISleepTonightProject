using UnityEngine;

public class InspectableObject : MonoBehaviour, IInteractable, IInspectable, IHiddenItemInteractable
{
    [Header("Inspection")]
    [SerializeField] private GameObject inspectPrefab;
    [SerializeField] private GameObject visualRoot;

    private HiddenItemHolder _itemHolder;

    private void Awake()
    {
        _itemHolder = GetComponent<HiddenItemHolder>();
    }

    public Transform GetInspectPivot() => transform;
    public GameObject GetInspectPrefab() => inspectPrefab;

    public void OnInspectionEnter()
    {
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    public void OnInspectionExit()
    {
        if (visualRoot != null)
            visualRoot.SetActive(true);
    }

    public virtual void Interact(Player player)
    {
        EventBus.Publish(new InspectionRequestedEvent
        {
            Target = this
        });
    }
    public HiddenItemHolder GetHiddenItemHolder()
    {
        return _itemHolder;
    }

    // =========================
    // IHiddenItemInteractable
    // =========================
    public void TryRevealItem(HiddenItemStateSO itemDefinition)
    {
        if (_itemHolder == null)
        {
            Debug.LogWarning($"[InspectableObject] HiddenItemHolder 없음: {name}");
            return;
        }

        _itemHolder.TryRevealItem(itemDefinition);
    }
}
