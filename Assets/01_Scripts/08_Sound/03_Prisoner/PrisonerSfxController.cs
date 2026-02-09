using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class PrisonerSfxController : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Hit Clips")]
    [SerializeField] private AudioClip[] hitClips;

    [Header("Attack Clips")]
    [SerializeField] private AudioClip[] attackClips;

    [Header("Moan Clips")]
    [SerializeField] private AudioClip[] moanClips;

    [Header("Die Clips")]
    [SerializeField] private AudioClip[] dieClips;

    // ★ [추가] 특수 상황용 1회성 클립 리스트 (Inspector 할당용)
    [Header("Special Clips (1-Shot)")]
    [SerializeField] private List<SpecialSoundData> specialClips;

    private const float HitVolume = 0.9f;
    private const float MoanVolume = 0.9f;
    private const float DieVolume = 1.0f;
    private const float AttackVolume = 1.0f;
    private const float SpatialBlend3D = 1f;
    private const float MoanCooldownSeconds = 0.25f;

    private AudioSource _hitSource;
    private AudioSource _voiceSource;
    private AudioSource _loopSource;

    private readonly List<int> _hitBag = new List<int>(16);
    private int _hitBagIndex;
    private readonly List<int> _attackBag = new List<int>(16);
    private int _attackBagIndex;
    private readonly List<int> _moanBag = new List<int>(16);
    private int _moanBagIndex;
    private readonly List<int> _dieBag = new List<int>(16);
    private int _dieBagIndex;

    private bool _diePlayed;
    private float _lastMoanTime;

    [Header("Loop Clips (Action Type 매핑)")]
    [SerializeField] private List<LoopSoundData> loopClips;
    private Dictionary<PrisonerAIType, AudioClip> _loopClipMap;

    // ★ [추가] 특수 클립 빠른 검색용 딕셔너리
    private Dictionary<string, AudioClip> _specialClipMap;

    private void Awake()
    {
        _hitSource = gameObject.AddComponent<AudioSource>();
        Setup3DOneShot(_hitSource);

        _voiceSource = gameObject.AddComponent<AudioSource>();
        Setup3DOneShot(_voiceSource);

        _hitSource.outputAudioMixerGroup = sfxMixerGroup;
        _voiceSource.outputAudioMixerGroup = sfxMixerGroup;

        RefillAndShuffleBag(hitClips, _hitBag, ref _hitBagIndex);
        RefillAndShuffleBag(moanClips, _moanBag, ref _moanBagIndex);
        RefillAndShuffleBag(dieClips, _dieBag, ref _dieBagIndex);
        RefillAndShuffleBag(attackClips, _attackBag, ref _attackBagIndex);

        _loopSource = gameObject.AddComponent<AudioSource>();
        Setup3DLoop(_loopSource);
        _loopSource.outputAudioMixerGroup = sfxMixerGroup;

        // 루프 딕셔너리 초기화
        _loopClipMap = new Dictionary<PrisonerAIType, AudioClip>();
        foreach (var data in loopClips)
        {
            if (!_loopClipMap.ContainsKey(data.type)) _loopClipMap.Add(data.type, data.clip);
        }

        // ★ [추가] 특수 클립 딕셔너리 초기화
        _specialClipMap = new Dictionary<string, AudioClip>();
        foreach (var data in specialClips)
        {
            if (!string.IsNullOrEmpty(data.key) && !_specialClipMap.ContainsKey(data.key))
            {
                _specialClipMap.Add(data.key, data.clip);
            }
        }
    }

    private static void Setup3DOneShot(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = SpatialBlend3D;
    }

    // ================================================================
    // ★ [추가] 특수 사운드 재생 (Key 기반)
    // ================================================================
    /// <summary>
    /// Inspector에 등록된 Special Clips 중 키값에 맞는 소리를 1회 재생합니다.
    /// </summary>
    public void PlaySpecialClip(string key, float volume = 1.0f)
    {
        if (_specialClipMap.TryGetValue(key, out AudioClip clip))
        {
            if (_voiceSource != null && clip != null)
            {
                _voiceSource.PlayOneShot(clip, volume);
            }
        }
        else
        {
            Debug.LogWarning($"[PrisonerSfxController] '{key}' 키에 해당하는 Special Clip이 없습니다.");
        }
    }

    public void PlayRandomAttack()
    {
        PlayFromBag(_hitSource, attackClips, _attackBag, ref _attackBagIndex, AttackVolume);
    }

    public void PlayHitAndRandomMoan()
    {
        PlayFromBag(_hitSource, hitClips, _hitBag, ref _hitBagIndex, HitVolume);
        if (Time.time - _lastMoanTime < MoanCooldownSeconds) return;

        if (moanClips != null && moanClips.Length > 0)
        {
            PlayFromBag(_voiceSource, moanClips, _moanBag, ref _moanBagIndex, MoanVolume);
            _lastMoanTime = Time.time;
        }
    }

    public void PlayRandomDieOnce()
    {
        if (_diePlayed) return;
        _diePlayed = true;
        PlayFromBag(_voiceSource, dieClips, _dieBag, ref _dieBagIndex, DieVolume);
    }

    private static void PlayFromBag(AudioSource source, AudioClip[] clips, List<int> bag, ref int bagIndex, float volume)
    {
        if (source == null || clips == null || clips.Length == 0) return;
        if (bag.Count != clips.Length || bagIndex >= bag.Count)
        {
            RefillAndShuffleBag(clips, bag, ref bagIndex);
        }

        int clipIndex = bag[bagIndex];
        bagIndex++;

        AudioClip clip = clips[clipIndex];
        if (clip != null) source.PlayOneShot(clip, volume);
    }

    private static void RefillAndShuffleBag(AudioClip[] clips, List<int> bag, ref int bagIndex)
    {
        bag.Clear();
        if (clips == null) return;
        for (int i = 0; i < clips.Length; i++) bag.Add(i);
        Shuffle(bag);
        bagIndex = 0;
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static void Setup3DLoop(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 1f;
        src.maxDistance = 15f;
    }

    public void PlayLoop(PrisonerAIType type)
    {
        if (_loopClipMap.TryGetValue(type, out AudioClip clip))
        {
            if (_loopSource.isPlaying && _loopSource.clip == clip) return;
            _loopSource.clip = clip;
            _loopSource.Play();
        }
        else
        {
            if (type != PrisonerAIType.Good && type != PrisonerAIType.Bad)
            {
                Debug.LogWarning($"[PrisonerSfxController] '{type}' LoopSoundData 누락!");
            }
            StopLoop();
        }
    }

    public void StopLoop()
    {
        if (_loopSource != null && _loopSource.isPlaying)
        {
            _loopSource.Stop();
            _loopSource.clip = null;
        }
        CancelInvoke();
        StopAllCoroutines();
    }

    public void StopAllSounds()
    {
        StopLoop();
        if (_hitSource != null) _hitSource.Stop();
        if (_voiceSource != null) _voiceSource.Stop();
    }
}

[System.Serializable]
public struct LoopSoundData
{
    public PrisonerAIType type;
    public AudioClip clip;
}

// ★ [추가] 특수 사운드용 데이터 구조체
[System.Serializable]
public struct SpecialSoundData
{
    public string key;       // 예: "Bikini_Pleasure"
    public AudioClip clip;
}