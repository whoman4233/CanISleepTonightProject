using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestNPCInteraction : MonoBehaviour , IInteractable
{
    [Header("대화 데이터")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    // 인터페이스 구현: 플레이어가 상호작용 키를 누르면 호출됨
    public void Interact(Player player)
    {
        if (dialogueData == null)
        {
            Debug.LogWarning($"{gameObject.name} 대화 데이터가 할당되지 않았습니다.");
            return;
        }
        if(dialogueManager != null )
        {
            dialogueManager.StartDialogue(dialogueData);
        }
        if(dialogueManager == null)
        {
            Debug.LogWarning("대화 매니저 찾지못함");
        }
    }
}
