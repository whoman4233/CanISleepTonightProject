using UnityEngine;

public class InspectableObject : MonoBehaviour, IInteractable, IInspectable
{
    [SerializeField] private GameObject inspectPrefab;

    public void Interact(Player player)
    {
        // Inspection 가능한 오브젝트라면 InspectionManager로 위임
        var inspectionManager = player.GetComponentInChildren<InspectionManager>();
        if (inspectionManager == null)
        {
            Debug.LogError("InspectionManager not found on Player");
            return;
        }

        inspectionManager.EnterInspection(this);
    }

    public GameObject GetInspectPrefab() => inspectPrefab;

    public void OnInspectionStart()
    {
        gameObject.SetActive(false); // 월드에서 숨김
    }

    public void OnInspectionEnd()
    {
        gameObject.SetActive(true); // 다시 월드에 표시
    }
}

