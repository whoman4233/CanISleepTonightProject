using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarryableBox : MonoBehaviour, ICarryable
{
    [Header("Prompt")]
    [SerializeField] private string carryPromptObjectType; //드는 오브젝트에 인스펙터 상으로 이름을 입력해야 프롬프트 출력으로 돌려줌.

    private Rigidbody rb;
    private Collider col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }
    public string GetCarryPromptObjectType() //프롬프트 출력용 메서드
    {
        return carryPromptObjectType;
    }
    public virtual void Interact(Player player) // 들기
    {
        var interactor = player.Interactor;
        if (interactor == null)
        {
            Debug.Log("interactor가 존재하지 않음");
            return;
        }
        rb.isKinematic = true; // 들면 물리,충돌 끄기
        col.enabled = false;
        transform.SetParent(interactor.CarryParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity; // 들었을 때 물체 회전값 0,0,0으로 맞춰줌 (들었을 때 똑바로 서라)

        interactor.SetHeldItem(this); // SetHeldItem에 들린 물체 넣어줌
        Debug.Log("물체 들기 완료");
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
}
