using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectRemoveAction : MonoBehaviour, IInspectAction
{
    [SerializeField] private GameObject visual;

    public void InspectAction(IInspectable owner)
    {
        if (visual != null)
            visual.SetActive(false);
    }
}
