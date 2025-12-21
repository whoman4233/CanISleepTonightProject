using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;

    private const string MasterVolumeParameterName = "MasterVolume";
    private const string BgmVolumeParameterName = "BGMVolume";
    private const string SfxVolumeParameterName = "SFXVolume";

    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const string IsMutedKey = "IsMuted";

    private const float MinVolumeDb = -80f;     // 완전 무음 수준
    private const float MinLinearVolume = 0.0001f;  // log10 계산용 최소값

    // 볼륨 값 (0 ~ 1)
    [Range(0f, 1f)] private float masterVolume = 1f;
    [Range(0f, 1f)] private float bgmVolume = 1f;
    [Range(0f, 1f)] private float sfxVolume = 1f;

    // 음소거 상태 확인용 변수
    private bool isMuted = false;

    private void Awake()
    {
        // 싱글톤 패턴
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ApplyVolumeSettings();
    }

    // 마스터 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    // SFX 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    // 음소거 토글
    public void ToggleMute()
    {
        isMuted = !isMuted;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    // 음소거 설정
    public void SetMute(bool mute)
    {
        isMuted = mute;
        ApplyVolumeSettings();
        SaveVolumeSettings();
    }

    // 실제 AudioSource에 볼륨 적용
    private void ApplyVolumeSettings()
    {
        float actualMasterVolume = isMuted ? 0f : masterVolume;

        SetMixerVolume(MasterVolumeParameterName, actualMasterVolume);
        SetMixerVolume(BgmVolumeParameterName, actualMasterVolume * bgmVolume);
        SetMixerVolume(SfxVolumeParameterName, actualMasterVolume * sfxVolume);

        if (bgmSource != null)
        {
            bgmSource.volume = 1f;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = 1f;
        }
    }

    // 볼륨 설정 저장 (PlayerPrefs 사용)
    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.SetInt(IsMutedKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // 볼륨 설정 로드
    private void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        bgmVolume = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        isMuted = PlayerPrefs.GetInt(IsMutedKey, 0) == 1;
    }

    // Getter 메서드들
    public float GetMasterVolume() => masterVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;
    public bool IsMuted() => isMuted;

    // BGM 재생
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }
    }

    // SFX 재생
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    private void SetMixerVolume(string parameterName, float normalizedVolume)
    {
        if (mainMixer == null)
        {
            Debug.LogWarning("AudioManager: mainMixer가 설정되지 않았습니다.", this);
            return;
        }

        float volume = Mathf.Clamp01(normalizedVolume);

        if (volume <= 0f)
        {
            mainMixer.SetFloat(parameterName, MinVolumeDb);
            return;
        }

        // 0~1 값을 dB 로 변환 (20 * log10)
        float volumeDb = Mathf.Log10(Mathf.Max(volume, MinLinearVolume)) * 20f;
        volumeDb = Mathf.Max(volumeDb, MinVolumeDb);

        mainMixer.SetFloat(parameterName, volumeDb);
    }
}

