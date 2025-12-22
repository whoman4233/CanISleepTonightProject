using System.Collections.Generic;
using UnityEngine;

public sealed class PrisonerSfxController : MonoBehaviour
{
    [Header("Hit Clips")]
    [SerializeField] private AudioClip[] hitClips;

    // 스크립트 내부에서만 설정
    private const float HitVolume = 0.9f;

    // 3D 사운드 기준값
    private const float SpatialBlend3D = 1f;

    private AudioSource _oneShotSource;

    // Shuffle Bag
    private readonly List<int> _bag = new List<int>(16);
    private int _bagIndex;

    private void Awake()
    {
        if (!TryGetComponent(out _oneShotSource))
        {
            _oneShotSource = gameObject.AddComponent<AudioSource>();
        }
        _oneShotSource.playOnAwake = false;
        _oneShotSource.loop = false;
        _oneShotSource.spatialBlend = SpatialBlend3D;

        RefillAndShuffleBag();
    }

    /// <summary>
    /// NPC가 맞았을 때 호출: Hit SFX를 "풀 소비 전 반복 없이" 랜덤 재생
    /// </summary>
    public void PlayRandomHit()
    {
        if (_oneShotSource == null) return;
        if (hitClips == null || hitClips.Length == 0) return;

 
        if (_bag.Count != hitClips.Length)
        {
            RefillAndShuffleBag();
        }

        // 다시 섞기
        if (_bagIndex >= _bag.Count)
        {
            RefillAndShuffleBag();
        }

        int clipIndex = _bag[_bagIndex];
        _bagIndex++;

        AudioClip clip = hitClips[clipIndex];
        if (clip == null) return;

        _oneShotSource.PlayOneShot(clip, HitVolume);
    }

    private void RefillAndShuffleBag()
    {
        _bag.Clear();

        if (hitClips == null) return;

        for (int i = 0; i < hitClips.Length; i++)
        {
            _bag.Add(i);
        }

        Shuffle(_bag);
        _bagIndex = 0;
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
}