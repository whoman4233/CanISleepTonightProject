using UnityEngine;

public sealed class PlayerSfxController : MonoBehaviour
{
    [Header("Loop Footstep")]
    [SerializeField] private AudioSource footstepLoopSource;
    [SerializeField] private AudioClip walkLoopClip;
    [SerializeField] private AudioClip runLoopClip;

    [Header("Jump / Land Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    // 볼륨은 스크립트 내부에서만 관리
    private const float JumpVolume = 0.9f;
    private const float LandVolume = 0.95f;

    [Header("Volumes")]
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float runVolume = 0.9f;
    [SerializeField] private float fadeSpeed = 8f;

    private const float MinMoveInputSqr = 0.01f;

    // Jump/Land 전용 원샷 소스 (Loop랑 분리)
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
        // Player에 AudioSource가 1개만 있을 수도 있으니 "추가 생성" 방식으로 안전하게
        _oneShotSource = gameObject.AddComponent<AudioSource>();
        _oneShotSource.playOnAwake = false;
        _oneShotSource.loop = false;

        // 3D 발소리 느낌을 유지하고 싶으면 동일하게
        _oneShotSource.spatialBlend = 1f;
    }

    public void TickFootstepLoop(float dt, Vector2 moveInput, bool isGrounded, bool isRunning, bool isCrouchMode)
    {
        if (footstepLoopSource == null) return;

        bool shouldHear =
            isGrounded &&
            !isCrouchMode &&
            moveInput.sqrMagnitude >= MinMoveInputSqr;

        AudioClip targetClip = isRunning ? runLoopClip : walkLoopClip;
        float targetVolume = isRunning ? runVolume : walkVolume;

        EnsurePlayingWithClip(targetClip);

        float desired = shouldHear ? targetVolume : 0f;
        footstepLoopSource.volume = Mathf.MoveTowards(footstepLoopSource.volume, desired, fadeSpeed * dt);
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
            if (!footstepLoopSource.isPlaying) footstepLoopSource.Play();
            return;
        }

        footstepLoopSource.clip = targetClip;
        footstepLoopSource.Play();
    }
}