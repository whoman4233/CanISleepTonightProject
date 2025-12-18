using UnityEngine;

public class InspectKnifeTarget : MonoBehaviour, IInspectTarget
{
    public void OnInspect(IInspectable owner)
    {
        Debug.Log("InspectKnifeTarget.OnInspect CALLED");

        if (owner is not TrashCan trashCan)
        {
            Debug.Log("Owner is not TrashCan");
            return;
        }

        trashCan.TakeKnife();
        gameObject.SetActive(false);
    }
}

