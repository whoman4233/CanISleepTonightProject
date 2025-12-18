using UnityEngine;

public class TrashCanInspectionView : MonoBehaviour, IInspectionView
{
    [SerializeField] private GameObject knifeObject;

    public void Bind(IInspectable owner)
    {
        if (owner is TrashCan trashCan)
        {
            knifeObject.SetActive(!trashCan.IsKnifeTaken);
        }
    }
}

