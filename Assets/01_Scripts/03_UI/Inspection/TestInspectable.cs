using UnityEngine;

public class TestInspectable : MonoBehaviour, IInspectable
{
    [SerializeField] private GameObject inspectPrefab;

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
