using UnityEngine;

public sealed class PlayerSfxController : MonoBehaviour
{
    [Header("Loop Footstep")]
    [SerializeField] private AudioSource footstepLoopSource;
    [SerializeField] private AudioClip walkLoopClip;
    [SerializeField] private AudioClip runLoopClip;

    [Header("Jump / Lands")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    [Header("Attack Swing")]
    [SerializeField] private AudioClip[] swingClips;

    // ==============================
    // 내부 설정값
    // ==============================
    private const float WalkVolume = 0.5f;
    private const float RunVolume = 0.7f;
    private const float FadeSpeed = 8f;

    private const float JumpVolume = 0.9f;
    private const float LandVolume = 0.95f;

    private const float MinMoveInputSqr = 0.01f;

    private const float SwingVolume = 0.85f;
    private int _lastSwingIndex = -1;


    // Jump / Land 전용 OneShot AudioSource
    private AudioSource _oneShotSource;

    private void Awake()
    {
        // ---- Footstep Loop ----
        if (footstepLoopSource != null)
        {
            footstepLoopSource.loop = true;
            footstepLoopSource.playOnAwake = false;
            footstepLoopSource.volume = 0f;

            if (footstepLoopSource.clip == null && walkLoopClip != null)
                footstepLoopSource.clip = walkLoopClip;

            if (footstepLoopSource.clip != null && !footstepLoopSource.isPlaying)
                footstepLoopSource.Play();
        }

        // ---- OneShot Source ----
        _oneShotSource = gameObject.AddComponent<AudioSource>();
        _oneShotSource.playOnAwake = false;
        _oneShotSource.loop = false;
        _oneShotSource.spatialBlend = 1f;
    }

    public void TickFootstepLoop(
        float dt,
        Vector2 moveInput,
        bool isGrounded,
        bool isRunning,
        bool isCrouchMode)
    {
        if (footstepLoopSource == null) return;

        bool shouldHear =
            isGrounded &&
            !isCrouchMode &&
            moveInput.sqrMagnitude >= MinMoveInputSqr;

        AudioClip targetClip = isRunning ? runLoopClip : walkLoopClip;
        float targetVolume = isRunning ? RunVolume : WalkVolume;

        EnsurePlayingWithClip(targetClip);

        float desired = shouldHear ? targetVolume : 0f;
        footstepLoopSource.volume = Mathf.MoveTowards(
            footstepLoopSource.volume,
            desired,
            FadeSpeed * dt
        );
    }

    public void PlayJumpSfx()
    {
        if (_oneShotSource == null || jumpClip == null) return;
        _oneShotSource.PlayOneShot(jumpClip, JumpVolume);
    }

    public void PlayLandSfx()
    {
        if (_oneShotSource == null || landClip == null) return;
        _oneShotSource.PlayOneShot(landClip, LandVolume);
    }

    private void EnsurePlayingWithClip(AudioClip targetClip)
    {
        if (targetClip == null || footstepLoopSource == null) return;

        if (footstepLoopSource.clip == targetClip)
        {
            if (!footstepLoopSource.isPlaying)
                footstepLoopSource.Play();
            return;
        }

        footstepLoopSource.clip = targetClip;
        footstepLoopSource.Play();
    }

    public void PlayAttackSwingSfx()
    {
        if (_oneShotSource == null) return;
        if (swingClips == null || swingClips.Length == 0) return;

        int index = Random.Range(0, swingClips.Length);

        // 같은 소리 연속 방지(클립이 2개 이상일 때만 의미)
        if (swingClips.Length > 1)
        {
            int safety = 0;
            while (index == _lastSwingIndex && safety < 10)
            {
                index = Random.Range(0, swingClips.Length);
                safety++;
            }
        }

        _lastSwingIndex = index;

        AudioClip clip = swingClips[index];
        if (clip == null) return;

        _oneShotSource.PlayOneShot(clip, SwingVolume);
    }
}