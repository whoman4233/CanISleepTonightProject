using System.Collections;
using UnityEngine;

public class SequenceExecutor : MonoBehaviour
{
    [Header("Post Processing / Vignette")]
    [SerializeField] private VignetteFadeController vignetteFade;

    [Header("플레이어 기본 도착 지점")]
    [SerializeField] private Transform defaultArrivalPoint;

    private void OnEnable()
    {
        // 연출 실행 요청 이벤트 구독
        EventBus.Subscribe<SequencePlayRequestedEvent>(OnPlayRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<SequencePlayRequestedEvent>(OnPlayRequested);
    }

    /// <summary>
    /// 연출 실행 요청 진입점
    /// </summary>
    private void OnPlayRequested(SequencePlayRequestedEvent e)
    {
        StartCoroutine(Co_PlaySequence(e));
    }

    /// <summary>
    /// 연출 시퀀스 실행 코루틴
    /// </summary>
    private IEnumerator Co_PlaySequence(SequencePlayRequestedEvent e)
    {
        SequenceOptionSO option = e.Sequence;
        if (option == null)
            yield break;

        DialogueManager dialogue = DialogueManager.Instance;
        Player player = FindAnyObjectByType<Player>();

        Transform targetPoint =
            e.TargetPoint != null ? e.TargetPoint : defaultArrivalPoint;

        // =========================
        // 1. 연출 전용 플레이어 Lock
        // - CharacterController 비활성화
        // - FSM Tick 차단
        // =========================
        EventBus.Publish(new PlayerCinematicLockRequestedEvent());

        // =========================
        // 2. 입력 잠금 (기존 GlobalInputLock)
        // =========================
        if (option.lockInput)
            EventBus.Publish(new GlobalInputLockRequestedEvent());

        // =========================
        // 3. 다이얼로그 (이동 전)
        // =========================
        if (option.playDialogue &&
            option.playDialogueBeforeMove &&
            dialogue != null &&
            !string.IsNullOrEmpty(option.dialogueKey))
        {
            dialogue.StartDialogueByKey(option.dialogueKey);

            if (option.waitDialogueEnd)
                yield return new WaitUntil(() => !dialogue.IsDialogueOpen);
        }

        // =========================
        // 4. Fade Out
        // =========================
        if (option.useFade && vignetteFade != null)
            yield return vignetteFade.FadeOut(this, option.fadeOutDuration);

        // =========================
        // 5. 플레이어 이동
        // =========================
        if (option.movePlayer && player != null && targetPoint != null)
        {
            Vector3 targetPos =
                targetPoint.position +
                targetPoint.forward * option.positionOffset.z +
                targetPoint.right * option.positionOffset.x +
                targetPoint.up * option.positionOffset.y;

            Quaternion targetRot = option.matchRotation
                ? targetPoint.rotation
                : player.transform.rotation;

            // CharacterController가 비활성 상태이므로
            // Transform 이동이 안전하게 적용됨
            player.transform.SetPositionAndRotation(targetPos, targetRot);
        }

        // =========================
        // 6. Fade In
        // =========================
        if (option.useFade && vignetteFade != null)
            yield return vignetteFade.FadeIn(this, option.fadeInDuration);

        // =========================
        // 7. 다이얼로그 (이동 후)
        // =========================
        if (option.playDialogue &&
            !option.playDialogueBeforeMove &&
            dialogue != null &&
            !string.IsNullOrEmpty(option.dialogueKey))
        {
            dialogue.StartDialogueByKey(option.dialogueKey);

            if (option.waitDialogueEnd)
                yield return new WaitUntil(() => !dialogue.IsDialogueOpen);
        }

        // =========================
        // 8. 입력 잠금 해제
        // =========================
        if (option.lockInput)
            EventBus.Publish(new GlobalInputLockReleasedEvent());

        // =========================
        // 9. 연출 전용 Lock 해제
        // =========================
        EventBus.Publish(new PlayerCinematicLockReleasedEvent());

        // =========================
        // 10. 미션 자동 보고 요청
        // - MissionBriefingNPC 쪽 흐름을 그대로 
        // =========================
        if (option.endMissionAfterSequence)
        {
            Debug.Log("[SequenceExecutor] AutoReport will be published in 1s");
            yield return new WaitForSecondsRealtime(0.2f);

            Debug.Log("[SequenceExecutor] Publish MissionAutoReportRequestedEvent");
            EventBus.Publish(new MissionAutoReportRequestedEvent());
        }
    }
}

