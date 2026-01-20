using UnityEngine;

[CreateAssetMenu(menuName = "Sequence/Sequence OptionSO")]
public class SequenceOptionSO : ScriptableObject
{
    [Header("입력 여부")]
    public bool lockInput = true;

    [Header("Fade / Volume")]
    public bool useFade = true;
    public float fadeOutDuration = 0.5f;
    public float fadeInDuration = 0.5f;

    [Header("플레이어 이동")]
    public bool movePlayer;
    public Vector3 positionOffset;
    public bool matchRotation = true;

    [Header("Dialogue")]
    public bool playDialogue;
    public string dialogueKey;

    [Header("Dialogue Timing")]
    [Tooltip("체크 시 이동 전에 다이얼로그 실행")]
    public bool playDialogueBeforeMove = true;

    [Header("Dialogue 종료 대기")]
    public bool waitDialogueEnd = true;

    [Header("미션 종료")]
    public bool endMissionAfterSequence;
    public bool isSuccess;
}
