using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PosterInteractable : MonoBehaviour, IInteractable
{
    [Header("SFX")]
    [SerializeField] private AudioClip takeClip;
    public void Interact(Player player)
    {
        AudioManager.Instance.PlaySFX(takeClip);
        gameObject.SetActive(false);
    }
}
