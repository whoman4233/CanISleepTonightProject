using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CabinetInteract : MonoBehaviour, IInteractable
{
    [Header("Animation Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private string triggerName = "Open"; // 애니메이터의 트라이거 파라미터 이름

    [Header("Baton")]
    [SerializeField] private GameObject baton;

    [Header("SFX")]
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    private bool isOpen = false;
    [SerializeField] private TutorialNPC _cachedNpc;


    // =========================
    // 아이템 획득 여부
    // =========================
    private bool isLootTaken = false;

    private void Awake()
    {
        baton.SetActive(false);

        // 설정 안했을 경우 자동으로 찾기
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // =========================
    // 프롬프트 상태 반환
    // - Cabinet은 Close / Open만 사용
    // =========================
    public OpenClosePromptState GetPromptStateEnum()
    {
        return isOpen
            ? OpenClosePromptState.Open
            : OpenClosePromptState.Close;
    }

    public void Interact(Player player)
    {
        if (animator == null)
            return;
        if (isLootTaken) return;
        bool wasOpen = isOpen; // 상태 토글 전 값 보관

        //TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (_cachedNpc == null) return;
        if (_cachedNpc.currentSubStep == DialogueKeys.DialogueType.BoxOpened)
        {
            isOpen = !isOpen;
            animator.SetBool("IsOpen", isOpen);

            // =========================
            // 최초 오픈 시에만 아이템 활성화
            // =========================
            if (!isLootTaken && isOpen)
            {
                baton.SetActive(true);
                isLootTaken = true;
            }
        }
    }

    // =====================================================
    // Animation Event 전용 메서드
    // - 애니메이션 타이밍에 맞춰 호출됨
    // =====================================================

    // [추가] 열기 사운드
    public void PlayOpenSFX()
    {
        AudioManager.Instance?.PlaySFX(openClip);
    }

    // [추가] 닫기 사운드
    public void PlayCloseSFX()
    {
        AudioManager.Instance?.PlaySFX(closeClip);
    }
}

