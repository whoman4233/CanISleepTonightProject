using UnityEngine;

public class TestInspectable : MonoBehaviour, IInspectable, IInteractable
{
    [SerializeField] private GameObject inspectPrefab;

    [SerializeField] private bool canInteract = true;
    public bool CanInteract => canInteract;

    public GameObject GetInspectPrefab()
    {
        return inspectPrefab;
    }

    public void OnInspectionStart()
    {
        gameObject.SetActive(false);
    }

    public void OnInspectionEnd()
    {
        gameObject.SetActive(true);
    }
}
