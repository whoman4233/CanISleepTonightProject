using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class PrisonerSfxController : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    [Header("Hit Clips")]
    [SerializeField] private AudioClip[] hitClips;

    [Header("Moan Clips")]
    [SerializeField] private AudioClip[] moanClips;

    [Header("Die Clips")]
    [SerializeField] private AudioClip[] dieClips;

    // 스크립트 내부 상수(매직넘버 방지)
    private const float HitVolume = 0.9f;
    private const float MoanVolume = 0.9f;
    private const float DieVolume = 1.0f;
    private const float SpatialBlend3D = 1f;

    // Moan이 너무 자주 나오지 않게(원하면 0으로 두면 매 히트마다 시도)
    private const float MoanCooldownSeconds = 0.25f;

    private AudioSource _hitSource;
    private AudioSource _voiceSource;

    // Shuffle bags
    private readonly List<int> _hitBag = new List<int>(16);
    private int _hitBagIndex;

    private readonly List<int> _moanBag = new List<int>(16);
    private int _moanBagIndex;

    private readonly List<int> _dieBag = new List<int>(16);
    private int _dieBagIndex;

    private bool _diePlayed;
    private float _lastMoanTime;

    [Header("Loop Clips (Action Type 매핑)")]
    [SerializeField] private List<LoopSoundData> loopClips; // Inspector 할당용

    // 딕셔너리: 타입을 넣으면 -> 클립이 나옴
    private Dictionary<PrisonerAIType, AudioClip> _loopClipMap;
    private AudioSource _loopSource; // 루프 재생 전용 소스

    private void Awake()
    {
        // Hit 전용 소스
        _hitSource = gameObject.AddComponent<AudioSource>();
        Setup3DOneShot(_hitSource);

        // 목소리/사망 전용 소스 (Hit와 겹쳐도 재생되게 분리)
        _voiceSource = gameObject.AddComponent<AudioSource>();
        Setup3DOneShot(_voiceSource);

        _hitSource.outputAudioMixerGroup = sfxMixerGroup;
        _voiceSource.outputAudioMixerGroup = sfxMixerGroup;

        RefillAndShuffleBag(hitClips, _hitBag, ref _hitBagIndex);
        RefillAndShuffleBag(moanClips, _moanBag, ref _moanBagIndex);
        RefillAndShuffleBag(dieClips, _dieBag, ref _dieBagIndex);

        // 1. 루프 전용 소스 추가
        _loopSource = gameObject.AddComponent<AudioSource>();
        Setup3DLoop(_loopSource); // 루프용 세팅
        _loopSource.outputAudioMixerGroup = sfxMixerGroup;

        // 2. 리스트 -> 딕셔너리 변환 (빠른 검색을 위해)
        _loopClipMap = new Dictionary<PrisonerAIType, AudioClip>();
        foreach (var data in loopClips)
        {
            if (!_loopClipMap.ContainsKey(data.type))
            {
                _loopClipMap.Add(data.type, data.clip);
            }
        }
    }

    private static void Setup3DOneShot(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = SpatialBlend3D;
    }

    /// <summary>
    /// 피격 시 호출: Hit은 항상, Moan은 랜덤으로 함께 재생
    /// </summary>
    public void PlayHitAndRandomMoan()
    {
        PlayFromBag(_hitSource, hitClips, _hitBag, ref _hitBagIndex, HitVolume);

        // Moan 쿨타임(너무 연타되면 과하게 들릴 수 있음)
        if (Time.time - _lastMoanTime < MoanCooldownSeconds)
            return;

        // Moan 클립이 있으면 1개 재생
        if (moanClips != null && moanClips.Length > 0)
        {
            PlayFromBag(_voiceSource, moanClips, _moanBag, ref _moanBagIndex, MoanVolume);
            _lastMoanTime = Time.time;
        }
    }

    /// <summary>
    /// 사망 시 호출: Die SFX를 랜덤으로 1회만 재생
    /// </summary>
    public void PlayRandomDieOnce()
    {
        if (_diePlayed) return;
        _diePlayed = true;

        PlayFromBag(_voiceSource, dieClips, _dieBag, ref _dieBagIndex, DieVolume);
    }

    // ====== Shuffle Bag 기반 재생 유틸 ======

    private static void PlayFromBag(
        AudioSource source,
        AudioClip[] clips,
        List<int> bag,
        ref int bagIndex,
        float volume)
    {
        if (source == null) return;
        if (clips == null || clips.Length == 0) return;

        if (bag.Count != clips.Length)
        {
            RefillAndShuffleBag(clips, bag, ref bagIndex);
        }

        if (bagIndex >= bag.Count)
        {
            RefillAndShuffleBag(clips, bag, ref bagIndex);
        }

        int clipIndex = bag[bagIndex];
        bagIndex++;

        AudioClip clip = clips[clipIndex];
        if (clip == null) return;

        source.PlayOneShot(clip, volume);
    }

    private static void RefillAndShuffleBag(AudioClip[] clips, List<int> bag, ref int bagIndex)
    {
        bag.Clear();

        if (clips == null) return;

        for (int i = 0; i < clips.Length; i++)
            bag.Add(i);

        Shuffle(bag);
        bagIndex = 0;
    }

    // Fisher-Yates shuffle
    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 루프 소스 세팅 (Loop = true)
    private static void Setup3DLoop(AudioSource src)
    {
        src.playOnAwake = false;
        src.loop = true; // ★ 중요
        src.spatialBlend = 1f; // 3D 사운드
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.minDistance = 1f;
        src.maxDistance = 15f;
    }

    public void PlayLoop(PrisonerAIType type)
    {
        // 1. 해당 타입에 맞는 클립이 있는지 확인
        if (_loopClipMap.TryGetValue(type, out AudioClip clip))
        {
            // 이미 같은 클립이 재생 중이면 무시 (끊김 방지)
            if (_loopSource.isPlaying && _loopSource.clip == clip) return;

            _loopSource.clip = clip;
            _loopSource.Play();
        }
        else
        {
            // 매핑된 소리가 없으면 그냥 멈춤 (예: Good, Bad 타입 등)
            StopLoop();
        }
    }

    // 소리 끄기
    public void StopLoop()
    {
        if (_loopSource.isPlaying)
        {
            _loopSource.Stop();
            _loopSource.clip = null;
        }
    }

    // (기존 StopAllLoops가 있었다면 이걸로 대체하거나 내부에서 호출)
    public void StopAllLoops() => StopLoop();
}

// Inspector에서 보기 위한 데이터 구조체
[System.Serializable]
public struct LoopSoundData
{
    public PrisonerAIType type; // 예: Singing
    public AudioClip clip;      // 예: Singing_Loop.mp3
}
