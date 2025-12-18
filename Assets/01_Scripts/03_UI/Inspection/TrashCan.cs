using UnityEngine;

public class TrashCan : MonoBehaviour, IInteractable, IInspectable
{
    [SerializeField] private GameObject inspectPrefab;
    private bool knifeTaken;

    public void Interact(Player player)
    {
        InspectionHelper.EnterInspection(player, this);
    }

    public void TakeKnife()
    {
        knifeTaken = true;
    }

    public bool IsKnifeTaken => knifeTaken;

    public GameObject GetInspectPrefab() => inspectPrefab;

    public void OnInspectionStart()
    {
        gameObject.SetActive(false);
    }

    public void OnInspectionEnd()
    {
        gameObject.SetActive(true);
    }
}

