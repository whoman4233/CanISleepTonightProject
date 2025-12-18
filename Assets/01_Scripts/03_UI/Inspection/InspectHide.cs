using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InspectTarget : MonoBehaviour, IInspectTarget
{
    public void OnInspect(IInspectable owner)
    {
        gameObject.SetActive(false);
    }
}
