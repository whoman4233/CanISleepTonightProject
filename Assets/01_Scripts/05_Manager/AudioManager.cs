using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;

    private const string MasterParam = "MasterVolume";
    private const string BgmParam = "BGMVolume";
    private const string SfxParam = "SFXVolume";
    private const string UiParam = "UIVolume";

    private const string UiKey = "UIVolume";
    private const string MasterKey = "MasterVolume";
    private const string BgmKey = "BGMVolume";
    private const string SfxKey = "SFXVolume";
    private const string MuteKey = "IsMuted";

    private const float MinDb = -80f;
    private const float MinLinear = 0.0001f;

    [Range(0f, 1f)] private float masterVolume = 1f;
    [Range(0f, 1f)] private float bgmVolume = 1f;
    [Range(0f, 1f)] private float sfxVolume = 1f;
    [Range(0f, 1f)] private float uiVolume = 0.8f;
    private bool isMuted;

    // =========================
    // UI 초기화용 Getter
    // =========================
    public float GetMasterVolume() => masterVolume;
    public float GetBgmVolume() => bgmVolume;
    public float GetSfxVolume() => sfxVolume;
    public float GetUiVolume() => uiVolume;
    public bool IsMuted() => isMuted;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyVolumes();
    }
    private void Start()
    {
        ApplyVolumes();
    }

    // =========================
    // UI 슬라이더용
    // ========================= 

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }

    public void SetBgmVolume(float value)
    {
        bgmVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }
    public void SetUiVolume(float value)
    {
        uiVolume = Mathf.Clamp01(value);
        ApplyVolumes();
        SaveSettings();
    }
    public void SetMute(bool mute)
    {
        isMuted = mute;
        ApplyVolumes();
        SaveSettings();
    }

    // =========================
    // BGM/SFX/UI
    // ========================= 

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null || clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
    public void PlaySFXLoop(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        // 같은 클립이 이미 루프 중이면 무시
        if (sfxSource.isPlaying && sfxSource.clip == clip && sfxSource.loop)
            return;

        sfxSource.clip = clip;
        sfxSource.loop = true;
        sfxSource.Play();
    }
    public void StopSFXLoop()
    {
        if (sfxSource == null)
            return;

        sfxSource.Stop();
        sfxSource.clip = null;
        sfxSource.loop = false;
    }

    public void PlayUISound(AudioClip clip)
    {
        if (uiSource == null || clip == null) return;
        uiSource.PlayOneShot(clip);
    }
    public void PlayUILoop(AudioClip clip)
    {
        if (uiSource == null || clip == null)
            return;

        // 같은 클립이 이미 루프 중이면 무시
        if (uiSource.isPlaying && uiSource.clip == clip && uiSource.loop)
            return;

        uiSource.clip = clip;
        uiSource.loop = true;
        uiSource.Play();
    }
    public void StopUILoop()
    {
        if (uiSource == null)
            return;

        uiSource.Stop();
        uiSource.clip = null;
        uiSource.loop = false;
    }


    // =========================
    // 볼륨/믹서 조절
    // ========================= 

    private void ApplyVolumes()
    {
        float master = isMuted ? 0f : masterVolume;

        SetMixerVolume(MasterParam, master);
        SetMixerVolume(BgmParam, master * bgmVolume);
        SetMixerVolume(SfxParam, master * sfxVolume);
        SetMixerVolume(UiParam, master * uiVolume);
        Debug.Log(
            $"[AudioManager] ApplyVolumes M:{masterVolume} B:{bgmVolume} S:{sfxVolume} Muted:{isMuted}"
        );
    }

    private void SetMixerVolume(string param, float linear)
    {
        if (mainMixer == null)
        {
            Debug.LogWarning("[AudioManager] Mixer missing");
            return;
        }

        if (linear <= 0f)
        {
            mainMixer.SetFloat(param, MinDb);
            return;
        }

        float db = Mathf.Log10(Mathf.Max(linear, MinLinear)) * 20f;
        db = Mathf.Max(db, MinDb);
        mainMixer.SetFloat(param, db);
    }

    // =========================
    // 저장/불러오기 
    // ========================= 

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterKey, masterVolume);
        PlayerPrefs.SetFloat(BgmKey, bgmVolume);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        PlayerPrefs.SetFloat(UiKey, uiVolume);
        PlayerPrefs.SetInt(MuteKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterKey, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BgmKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        uiVolume = PlayerPrefs.GetFloat(UiKey, 1f);
        isMuted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
    }
}


