using UnityEngine;

/// <summary>
/// Mission 4 전용 다이얼로그 트리거
/// - 플레이어가 진입하면 강제로 다이얼로그 재생
/// - 동일 미션 내에서 1회만 실행됨
/// - 상태 관리는 Mission_FindImposterStrategy가 담당
/// </summary>
[RequireComponent(typeof(Collider))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("Trigger Id (Mission 4 내부에서 유니크해야 함)")]
    [SerializeField] private string triggerId;

    [Header("Dialogue Data")]
    [SerializeField] private TriggerDialogueSO dialogue;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 아니면 무시
        if (!other.CompareTag("Player"))
            return;

        // 현재 미션이 Mission 4인지 확인
        var mission =
            DailyMissionManager.Instance?.CurrentMission
            as Mission_FindImposterStrategy;

        if (mission == null)
            return;

        // 이미 사용된 트리거면 무시
        mission.MarkTriggerUsed(triggerId);

        // 트리거 사용 처리 (미션 쪽에서 상태 관리)
        mission.MarkTriggerUsed(triggerId);

        // 다이얼로그 강제 재생
        DialogueManager.Instance.StartDialogue(dialogue.Lines);

        // 사용 후 파괴
        Destroy(gameObject);
    }
}

