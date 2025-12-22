using UnityEngine;

public sealed class PlayerSfxController : MonoBehaviour
{
    [Header("Loop Footstep")]
    [SerializeField] private AudioSource footstepLoopSource;
    [SerializeField] private AudioClip walkLoopClip;
    [SerializeField] private AudioClip runLoopClip;

    [Header("OneShot Source (Jump/Land/Swing)")]
    [SerializeField] private AudioSource oneShotSource;

    [Header("Jump / Lands")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    [Header("Attack Swing")]
    [SerializeField] private AudioClip[] swingClips;

    // 내부 설정값
    private const float WalkVolume = 0.5f;
    private const float RunVolume = 0.7f;
    private const float FadeSpeed = 8f;

    private const float JumpVolume = 0.9f;
    private const float LandVolume = 0.95f;
    private const float SwingVolume = 0.85f;

    private const float MinMoveInputSqr = 0.01f;
    private const float SpatialBlend3D = 1f;

    private int _lastSwingIndex = -1;

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
        if (oneShotSource == null)
        {
            Debug.LogWarning("[PlayerSfxController] oneShotSource is not assigned. (Jump/Land/Swing SFX will not play)");
            return;
        }

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = SpatialBlend3D;
    }

    public void TickFootstepLoop(float dt, Vector2 moveInput, bool isGrounded, bool isRunning, bool isCrouchMode)
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
        footstepLoopSource.volume = Mathf.MoveTowards(footstepLoopSource.volume, desired, FadeSpeed * dt);
    }

    public void PlayJumpSfx()
    {
        if (oneShotSource == null || jumpClip == null) return;
        oneShotSource.PlayOneShot(jumpClip, JumpVolume);
    }

    public void PlayLandSfx()
    {
        if (oneShotSource == null || landClip == null) return;
        oneShotSource.PlayOneShot(landClip, LandVolume);
    }

    public void PlayAttackSwingSfx()
    {
        if (oneShotSource == null) return;
        if (swingClips == null || swingClips.Length == 0) return;

        int index = Random.Range(0, swingClips.Length);

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

        oneShotSource.PlayOneShot(clip, SwingVolume);
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
}