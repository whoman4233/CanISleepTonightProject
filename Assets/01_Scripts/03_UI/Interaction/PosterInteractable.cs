using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PosterInteractable : MonoBehaviour, IInteractable
{
    public void Interact(Player player)
    {
        gameObject.SetActive(false);
    }
}
