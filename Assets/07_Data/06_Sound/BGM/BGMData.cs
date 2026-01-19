using UnityEngine;

[CreateAssetMenu(menuName = "Audio/BGM Data")]
public class BGMData : ScriptableObject
{
    [Header("Phase")]
    public GamePhase phase;

    [Header("Audio")]
    public AudioClip clip;
    public bool loop = true;

    [Header("Fade")]
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;

    [Header("Priority")]
    public int priority = 0;
}
