using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TutorialNPC;


public class TutorialCarryEventTrigger : MonoBehaviour , ICarryable
{
    [Header("Prompt")]
    [SerializeField] private string carryPromptObjectType = "Pillow";

    public DialogueKeys.DialogueType stepToPublish; // 인스펙터에서 설정 (예: BoxOpened)
    private bool _isTriggered = false; // 실행 여부 체크

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void Interact(Player player) // 그냥 오브젝트에 상호작용하면 바로 스텝 넘어가게
    {
        var interactor = player.Interactor;
        if (interactor == null)
        {
            Debug.Log("interactor가 존재하지 않음");
            return;
        }
        TutorialNPC npc = FindObjectOfType<TutorialNPC>();
        if (npc == null) return;
        rb.isKinematic = true; // 들면 물리,충돌 끄기
        col.enabled = false;
        transform.SetParent(interactor.CarryParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity; // 들었을 때 물체 회전값 0,0,0으로 맞춰줌 (들었을 때 똑바로 서라)

        interactor.SetHeldItem(this); // SetHeldItem에 들린 물체 넣어줌
        Debug.Log("물체 들기 완료");
        if (!_isTriggered && (int)npc.currentSubStep == (int)stepToPublish - 1)
        {
            EventBus.Publish(new DialogueStepChangedEvent(stepToPublish));
            _isTriggered = true;
            Debug.Log("튜토리얼 이벤트 발행 완료");
        }
        else
        {
            Debug.Log("현재 스텝이 맞지 않거나 이벤트가 이미 발행되었습니다");
        }
    }

    public void Drop(Player player) // 놓기
    {
        var interactor = player.Interactor;

        transform.SetParent(null);
        rb.isKinematic = false;
        col.enabled = true;

        if (interactor != null) interactor.ClearHeldItem(); // 비워줌

        // 던지기
        rb.AddForce(player.transform.forward * 2f, ForceMode.Impulse); // 추후 수치조정
        Debug.Log("물체 놓기 완료");
    }

    public string GetCarryPromptObjectType()
    {
        return carryPromptObjectType;
    }
}
