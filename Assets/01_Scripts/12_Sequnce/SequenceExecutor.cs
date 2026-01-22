using System;
using System.Collections;
using UnityEngine;

public class SequenceExecutor : MonoBehaviour
{
    // =========================
    // Singleton
    // =========================
    public static SequenceExecutor Instance { get; private set; }

    [Header("Post Processing / Vignette")]
    [SerializeField] private VignetteFadeController vignetteFade;

    [Header("플레이어 기본 도착 지점")]
    [SerializeField] private Transform defaultArrivalPoint;

    private Action<SequencePlayRequestedEvent> _onPlayRequested;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _onPlayRequested = OnPlayRequested;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPlayRequested);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPlayRequested);
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

        // =========================
        // Scene-safe 참조 확보
        // =========================
        DialogueManager dialogue = null;
        Player player = null;

        // 씬 로드 직후일 수 있으므로 대기
        while (dialogue == null || player == null)
        {
            dialogue = DialogueManager.Instance;
            player = FindAnyObjectByType<Player>();
            yield return null;
        }

        Transform targetPoint =
            e.TargetPoint != null ? e.TargetPoint : defaultArrivalPoint;

        // =========================
        // 1. 연출 전용 플레이어 Lock
        // =========================
        EventBus.Publish(new PlayerCinematicLockRequestedEvent());

        // =========================
        // 2. 입력 잠금
        // =========================
        if (option.lockInput)
            EventBus.Publish(new GlobalInputLockRequestedEvent());

        // =========================
        // 3. 다이얼로그 (이동 전)
        // =========================
        if (option.playDialogue &&
            option.playDialogueBeforeMove &&
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
        if (option.movePlayer && targetPoint != null)
        {
            Vector3 targetPos =
                targetPoint.position +
                targetPoint.forward * option.positionOffset.z +
                targetPoint.right * option.positionOffset.x +
                targetPoint.up * option.positionOffset.y;

            Quaternion targetRot = option.matchRotation
                ? targetPoint.rotation
                : player.transform.rotation;

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
        // =========================
        if (option.endMissionAfterSequence)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            EventBus.Publish(new MissionAutoReportRequestedEvent());
        }
    }
}


