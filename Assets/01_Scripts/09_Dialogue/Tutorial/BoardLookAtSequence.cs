using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class BoardLookAtSequence : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform boardTarget; // 바라볼 타겟 (화이트보드)
    [SerializeField] private float lookSpeed = 2.0f;         // 보드를 향해 회전하는 속도
    [SerializeField] private float returnSpeed = 3.0f;       // 원래대로 돌아오는 속도
    [SerializeField] private float waitTime = 2.5f;          // 보드를 주시하는 시간

    public void StartSequence()
    {
        Player player = FindObjectOfType<Player>();
        if (player != null)
        {
            StartCoroutine(LookAtRoutine(player));
        }
        else
        {
            Debug.LogError("[Sequence] 플레이어를 찾을 수 없습니다!");
        }
    }

    private IEnumerator LookAtRoutine(Player player)
    {
        // 잠금 및 일시정지
        EventBus.Publish(new GlobalInputLockRequestedEvent());
        EventBus.Publish(new DialogueStepChangedEvent(DialogueKeys.DialogueType.BoardSee));
        player.StateMachine.SetPaused(true); // FSM 정지
        var brain = Camera.main.GetComponent<CinemachineBrain>(); // 카메라 제어권 뺏기
        if (brain != null)
        {
            brain.enabled = false;
        }
        Transform camTransform = Camera.main.transform;
        Quaternion startRot = camTransform.rotation;
        Vector3 dir = (boardTarget.position - camTransform.position).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dir);

        // 2. 보드 보기
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * lookSpeed;
            camTransform.rotation = Quaternion.Slerp(startRot, targetRot, elapsed);
            yield return null;
        }

        yield return new WaitForSeconds(waitTime);

        // 복귀
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * lookSpeed;
            camTransform.rotation = Quaternion.Slerp(targetRot, startRot, elapsed);
            yield return null;
        }

        // 시선 동기화 후 정지 해제
        if (brain != null) brain.enabled = true; // 카메라 제어권 돌려주기
        player.StateMachine.SetPaused(false);
        EventBus.Publish(new GlobalInputLockReleasedEvent());
        DialogueManager.Instance.StartDialogueByKeys(DialogueKeys.Speakers.Frank, DialogueKeys.DialogueType.BoardSee.ToString());
    }
}
