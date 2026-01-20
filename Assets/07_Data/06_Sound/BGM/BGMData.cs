using UnityEngine;

[CreateAssetMenu(menuName = "Audio/BGM Data")]
public class BGMData : ScriptableObject
{
    [Header("Scene")]
    public string sceneName;   // Scene 이름과 1:1 매핑

    [Header("Audio")]
    public AudioClip clip;
    public bool loop = true;

    [Header("Fade")]
    public float fadeInTime = 1.0f;
    public float fadeOutTime = 1.0f;

    [Header("Priority")]
    public int priority = 0;
}
