using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MimiTrigger : MonoBehaviour
{
    private bool _triggered = false;
    [SerializeField] private float detectionRange = 3.0f;
    [SerializeField] private LayerMask playerLayer;

    private void Update()
    {
        // 이미 트리거됐으면 리턴
        if (_triggered) return;

        // 레이어를 이용해 물리적으로 감지
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRange, playerLayer);

        foreach (var hit in hitColliders) // 태그까지 검사
        {
            if (hit.CompareTag("Player"))
            {
                _triggered = true;
                Debug.Log($"플레이어 감지 및 대사 시작");
                ExecuteDialogue(hit);
                break;
            }
        }
    }

    private void ExecuteDialogue(Collider playerCollider)
    {
        var fsm = GetComponentInParent<PrisonerFSM>();
        if (fsm != null)
        {
            fsm.SetPlayerReference(playerCollider.transform);
            fsm.ChangeState(fsm.BikiniState);
            Debug.Log("비키니상태로 전환");
        }
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogueByKeys(
                DialogueKeys.Speakers.Mimi,
                DialogueKeys.Types.Dialogue,
                () => EventBus.Publish(new Mission03DialogueEnded()) // 대사 끝나면 이벤트 발행
            );
        }
    }

    // 범위 시각화 (인스펙터에서 범위 조절 시 편리함)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

}
