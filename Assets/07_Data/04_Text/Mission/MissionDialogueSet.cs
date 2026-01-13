using UnityEngine;

[CreateAssetMenu(menuName = "Mission/Mission Dialogue Set")]
public class MissionDialogueSet : ScriptableObject
{
    [Header("Mission Theme")]
    public MissionDayTheme theme;

    [Header("Dialogue")]
    public DialogueData briefing;

    public DialogueData reportSuccess;
    public DialogueData reportFail;
}
