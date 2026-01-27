using UnityEngine;
using System;
using System.Collections;

public class PrisonerQTEAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [SerializeField] private string qteStateParam = "QTEState";
    [SerializeField] private string hitTrigger = "HitSuccess";
    [SerializeField] private string attackTrigger = "AttackFail";

    [Header("QTE SFX")]
    [SerializeField] private AudioClip qteStartSfx;
    [SerializeField] private AudioClip qteLoopSfx;
    [SerializeField] private AudioClip hitSfx;

    private int _qteStateHash;
    private int _hitHash;
    private int _attackHash;

    // 현재 이 죄수에게 걸린 QTE 액션
    private QTEActionSO _myAction;

    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _qteStateHash = Animator.StringToHash(qteStateParam);
        _hitHash = Animator.StringToHash(hitTrigger);
        _attackHash = Animator.StringToHash(attackTrigger);

        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onQTEStarted);
        EventBus.Subscribe(_onQTEEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onQTEStarted);
        EventBus.Unsubscribe(_onQTEEnded);
    }

    // =========================
    // QTE Flow
    // =========================

    private void OnQTEStarted(QTEStartedEvent e)
    {
        // 이 죄수에게 들어온 QTE 액션 저장
        _myAction = e.Action;

        // 이전 QTE 결과 트리거 잔여 제거
        animator.ResetTrigger(_hitHash);
        animator.ResetTrigger(_attackHash);

        // QTE 시작 SFX (1회)
        if (qteStartSfx != null)
            AudioManager.Instance?.PlaySFX(qteStartSfx);

        // QTE 루프 SFX 시작
        if (qteLoopSfx != null)
            AudioManager.Instance?.PlaySFXLoop(qteLoopSfx);

        // QTE 진입 (Any State → Start)
        PlayIntro();
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        // 다른 QTE의 종료 이벤트는 무시
        if (e.Action != _myAction)
            return;

        _myAction = null;

        // QTE 루프 SFX 종료
        AudioManager.Instance?.StopSFXLoop();

        // QTE 상태 종료
        StopQTE();

        // [1] 결과 트리거 발사 (단 1회)
        if (e.Result == QTEResult.Success)
            animator.SetTrigger(_hitHash);
        else
            animator.SetTrigger(_attackHash);

        // [2] 한 프레임 뒤 트리거 리셋 (재소비 방지)
        StartCoroutine(Co_ResetResultTriggers());
    }

    public void PlayIntro()
    {
        // Any State → PSN_AA_Pounce_Start 조건 충족
        animator.SetInteger(_qteStateHash, 1);
    }

    public void PlayLoop()
    {
        // Start 재진입 차단 + QTE 루프 상태
        animator.SetInteger(_qteStateHash, 2);
    }

    public void StopQTE()
    {
        // QTE 완전 종료
        animator.SetInteger(_qteStateHash, 0);
    }

    // =========================
    // Result
    // =========================

    public void PlayHitSuccess()
    {
        animator.SetTrigger(_hitHash);
    }

    public void PlayAttackFail()
    {
        animator.SetTrigger(_attackHash);
    }

    // =========================
    // Animation Events
    // =========================

    // PSN_AA_Pounce_Start 클립 끝 프레임에 연결
    public void OnPounceStartFinished()
    {
        // Start → Progress 전환 시점
        PlayLoop();
    }

    public void OnPrisonerHitFrame()
    {
        EventBus.Publish(new PrisonerHitTimingEvent());
    }

    public void OnPrisonerAttackFrame()
    {
        EventBus.Publish(new PlayerAttackTimingEvent());
    }
    // =========================
    // Internal
    // =========================

    // 결과 트리거 재사용 방지용
    private IEnumerator Co_ResetResultTriggers()
    {
        // Animator가 전이를 소비하도록 1프레임 대기
        yield return null;

        animator.ResetTrigger(_hitHash);
        animator.ResetTrigger(_attackHash);
    }
    public void OnPrisonerHitSfx()
    {
        if (hitSfx != null)
            AudioManager.Instance?.PlaySFX(hitSfx);
    }
}




