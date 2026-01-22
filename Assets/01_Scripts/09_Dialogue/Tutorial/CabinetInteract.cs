using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CabinetInteract : MonoBehaviour , IInteractable
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Open"; // 애니메이터의 트라이거 파라미터 이름

    [Header("Baton")]
    [SerializeField] private GameObject baton;

    [Header("SFX")]
    [SerializeField] private AudioClip openClip;

    private bool isOpen = false;

    private void Awake()
    {
        baton.SetActive(false);
        // 설정 안했을 경우 자동으로 찾기
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public void Interact(Player player)
    {
        if (animator == null) return;
        isOpen = !isOpen;
        animator.SetBool("IsOpen", isOpen);
        AudioManager.Instance.PlaySFX(openClip);
        baton.SetActive(true);
    }
}
