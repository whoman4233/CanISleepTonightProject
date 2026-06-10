using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarryableBox : MonoBehaviour, ICarryable
{
    [Header("Prompt")]
    [SerializeField] private string carryPromptObjectType; //드는 오브젝트에 인스펙터 상으로 이름을 입력해야 프롬프트 출력으로 돌려줌.

    [Header("SFX")]
    [SerializeField] private InteractionSfxRuleTableSO sfxRuleTable;
    [SerializeField] private LayerMask groundLayerMask;

    private Rigidbody rb;
    private Collider col;

    private bool _isDropping;
    private bool _landed;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        if (rb != null)
        {
            // 1. 시작하자마자 Kinematic으로 물리 연산을 아예 끕니다.
            rb.isKinematic = true;

            // 2. 0.5초 뒤에 위치가 안정화되면 물리 연산을 켭니다.
            StartCoroutine(EnablePhysicsDelayed(rb));
        }
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
        // =========================
        // PickUp SFX
        // =========================
        PlayInteractionSfx(InteractionState.CanPickUp);

        rb.isKinematic = true; // 들면 물리,충돌 끄기
        col.enabled = false;
        transform.SetParent(interactor.CarryParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity; // 들었을 때 물체 회전값 0,0,0으로 맞춰줌 (들었을 때 똑바로 서라)

        interactor.SetHeldItem(this); // SetHeldItem에 들린 물체 넣어줌
        // Drop 상태 리셋
        _isDropping = false;
        _landed = false;
        Debug.Log("물체 들기 완료");
    }

    public virtual void Drop(Player player) // 놓기
    {
        var interactor = player.Interactor;

        transform.SetParent(null);
        rb.isKinematic = false;
        col.enabled = true;

        if (interactor != null) interactor.ClearHeldItem(); // 비워줌

        // 던지기
        rb.AddForce(player.transform.forward * 2f, ForceMode.Impulse); // 추후 수치조정

        // =========================
        // Drop 상태 진입
        // =========================
        _isDropping = true;
        _landed = false;

        Debug.Log("물체 놓기 완료");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isDropping || _landed)
            return;

        // 바닥 판정 (레이어 or 태그 중 택1)
        if ((groundLayerMask.value & (1 << collision.collider.gameObject.layer)) == 0)
            return;

        // 충돌 강도 체크 (너무 약하면 무시)
        float impact = collision.relativeVelocity.magnitude;
        if (impact < 1.2f)
            return;

        _landed = true;
        _isDropping = false;

        PlayInteractionSfx(InteractionState.CanDrop);
    }

    private void PlayInteractionSfx(InteractionState state)
    {
        if (sfxRuleTable == null)
            return;

        var clip = sfxRuleTable.GetClip(carryPromptObjectType, state);
        if (clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
    private IEnumerator EnablePhysicsDelayed(Rigidbody rb)
    {
        // 0.5초면 씬 로딩 후 물리 엔진이 바닥 위치를 파악하기에 충분한 시간입니다.
        yield return new WaitForSeconds(0.5f);
        rb.isKinematic = false;

        // 만약 여전히 조금 떨린다면 Sleep을 강제합니다.
        rb.Sleep();
    }
}
