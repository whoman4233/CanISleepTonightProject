using UnityEngine;

public class InspectableObject : MonoBehaviour, IInteractable, IInspectable
{
    [Header("Inspection")]
    [SerializeField] private GameObject inspectPrefab;
    [SerializeField] private GameObject visualRoot;

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

    public void Interact(Player player)
    {
        player.TryEnterInspection(this);
    }
}
