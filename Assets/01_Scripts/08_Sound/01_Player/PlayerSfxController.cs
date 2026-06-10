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

    [Header("Jump Landing (After Jump)")]
    [SerializeField] private AudioClip jumpLandingClip;

    [Header("Attack Swing")]
    [SerializeField] private AudioClip[] swingClips;

    [Header("Hit (Player Damaged)")]
    [SerializeField] private AudioClip[] hitClips;

    // 내부 설정값
    private const float WalkVolume = 0.5f;
    private const float RunVolume = 0.7f;
    private const float FadeSpeed = 8f;
    private const float CrouchVolume = 0.3f;

    private const float JumpVolume = 0.9f;
    private const float LandVolume = 0.95f;

    private const float JumpLandingVolume = 0.7f;

    private const float SwingVolume = 0.85f;

    private const float HitVolume = 0.9f;

    private const float MinMoveInputSqr = 0.01f;
    private const float SpatialBlend3D = 1f;

    private const int MaxReselectAttempts = 10; // (기존 safety < 10 매직넘버 제거)

    private int _lastSwingIndex = -1; //타격 사운드 목록

    private int _lastHitIndex = -1; // 데미지 피드백 사운드 목록

    private void Awake()
    {
        // ---- Footstep Loop ----
        if (footstepLoopSource != null)
        {
            footstepLoopSource.loop = true;
            footstepLoopSource.playOnAwake = false;
            footstepLoopSource.volume = 0f;

            // 시작 clip이 비어있다면 기본으로 Walk를 세팅만 해둠 (Play는 하지 않음)
            if (footstepLoopSource.clip == null && walkLoopClip != null)
                footstepLoopSource.clip = walkLoopClip;
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
        if (footstepLoopSource == null)
            return;

        bool hasMoveInput = moveInput.sqrMagnitude >= MinMoveInputSqr;

        // ---------------------------------
        // 1. 이동 불가 → 볼륨만 0 (Stop 금지)
        // ---------------------------------
        if (!isGrounded || !hasMoveInput)
        {
            footstepLoopSource.volume = Mathf.MoveTowards(
                footstepLoopSource.volume,
                0f,
                FadeSpeed * dt
            );
            return;
        }

        // ---------------------------------
        // 2. 상태별 클립 / 볼륨
        // ---------------------------------
        AudioClip targetClip;
        float targetVolume;

        if (isCrouchMode)
        {
            targetClip = walkLoopClip;
            targetVolume = CrouchVolume;
        }
        else if (isRunning)
        {
            targetClip = runLoopClip;
            targetVolume = RunVolume;
        }
        else
        {
            targetClip = walkLoopClip;
            targetVolume = WalkVolume;
        }

        // ---------------------------------
        // 3. 클립 변경 시에만 Stop + Play
        // ---------------------------------
        if (footstepLoopSource.clip != targetClip)
        {
            footstepLoopSource.Stop();
            footstepLoopSource.clip = targetClip;
            footstepLoopSource.volume = 0f;
            footstepLoopSource.Play();
        }
        else if (!footstepLoopSource.isPlaying)
        {
            // 같은 clip인데 Stop된 경우 복구
            footstepLoopSource.Play();
        }

        // ---------------------------------
        // 4. 볼륨 보간
        // ---------------------------------
        footstepLoopSource.volume = Mathf.MoveTowards(
            footstepLoopSource.volume,
            targetVolume,
            FadeSpeed * dt
        );
    }

    public void PlayJumpSfx()
    {
        if (oneShotSource == null || jumpClip == null) return;
        oneShotSource.PlayOneShot(jumpClip, JumpVolume);
    }

    public void PlayJumpLandingSfx()
    {
        if (oneShotSource == null || jumpLandingClip == null) return;
        oneShotSource.PlayOneShot(jumpLandingClip, JumpLandingVolume);
    }

    // Animation Event에서 직접 호출
    public void AE_PlayJumpLanding()
    {
        PlayJumpLandingSfx();
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
            int attempts = 0;
            while (index == _lastSwingIndex && attempts < MaxReselectAttempts)
            {
                index = Random.Range(0, swingClips.Length);
                attempts++;
            }
        }

        _lastSwingIndex = index;

        AudioClip clip = swingClips[index];
        if (clip == null) return;

        oneShotSource.PlayOneShot(clip, SwingVolume);
    }

    public void PlayHitRandomSfx()
    {
        if (oneShotSource == null) return;
        if (hitClips == null || hitClips.Length == 0) return;

        int index = Random.Range(0, hitClips.Length);

        if (hitClips.Length > 1)
        {
            int attempts = 0;
            while (index == _lastHitIndex && attempts < MaxReselectAttempts)
            {
                index = Random.Range(0, hitClips.Length);
                attempts++;
            }
        }

        _lastHitIndex = index;

        AudioClip clip = hitClips[index];
        if (clip == null) return;

        oneShotSource.PlayOneShot(clip, HitVolume);
    }

    private void EnsurePlayingWithClip(AudioClip targetClip)
    {
        if (targetClip == null || footstepLoopSource == null) return;

        if (footstepLoopSource.clip != targetClip)
            footstepLoopSource.clip = targetClip;

        // “필요할 때만” 재생 (Awake에서 미리 Play하지 않음)
        if (!footstepLoopSource.isPlaying)
            footstepLoopSource.Play();
    }

    public void StopFootstepLoopImmediate()
    {
        if (footstepLoopSource == null) return;

        footstepLoopSource.volume = 0f;

        // 완전히 멈춰서 공중에서 “루프 자체”가 안 돌게 함
        if (footstepLoopSource.isPlaying)
            footstepLoopSource.Stop();
    }
}