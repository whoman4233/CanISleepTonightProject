using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartPhoneInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Player player)
    {
        gameObject.SetActive(false);
    }
}
