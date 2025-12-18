using UnityEngine;

public class TestInspectable : MonoBehaviour, IInspectable
{
    [SerializeField] private GameObject inspectPrefab;
    [SerializeField] private GameObject visualRoot;

    [SerializeField] private bool canInteract = true;
    public bool CanInteract => canInteract;

    public GameObject GetInspectPrefab()
    {
        return inspectPrefab;
    }

    public void OnInspectionStart()
    {
        // 필드에서는 안 보이게만
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    public void OnInspectionEnd()
    {
        if (visualRoot != null)
            visualRoot.SetActive(true);
    }
}
