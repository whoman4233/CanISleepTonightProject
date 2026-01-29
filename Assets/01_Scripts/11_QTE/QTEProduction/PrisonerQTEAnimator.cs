using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 죄수 QTE 전용 애니메이션 컨트롤러
///

public class PrisonerQTEAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Header("Animator Params")]
    [Tooltip("QTE 진행 상태를 제어하는 int 파라미터")]
    [SerializeField] private string qteStateParam = "QTEState";

    [Header("QTE SFX")]
    [SerializeField] private AudioClip qteStartSfx;
    [SerializeField] private AudioClip qteLoopSfx;
    [SerializeField] private AudioClip hitSfx;

    [Header("QTE Layer")]
    [Tooltip("Animator에서 QTE 전용 레이어 인덱스")]
    [SerializeField] private int qteLayerIndex = 1;

    // Animator Hash
    private int _qteStateHash;

    // 현재 이 죄수에게 적용된 QTE 액션
    private QTEActionSO _myAction;

    // 이벤트 핸들러 캐싱
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    /*
     * QTEState 값 정의 (Animator와 반드시 동일해야 함)
     *
     * 0 : Idle (QTE 아님)
     * 1 : Start        (QTE 시작)
     * 2 : Progress     (QTE 진행 중)
     * 3 : Success      (QTE 성공 결과)
     * 4 : Fail         (QTE 실패 결과)
     */

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        _qteStateHash = Animator.StringToHash(qteStateParam);

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

    // ======================================================
    // QTE Lifecycle
    // ======================================================

    /// <summary>
    /// QTE 시작 이벤트 수신
    /// - QTE Layer 활성화
    /// - QTEState를 Start로 설정
    /// </summary>
    private void OnQTEStarted(QTEStartedEvent e)
    {
        // 이 죄수가 공격자인지 확인
        if (PrisonerQTEContext.CurrentAttackerQTEAnimator != this)
            return;

        _myAction = e.Action;

        // QTE 시작 직전, 플레이어를 바라보도록 회전 보정
        FacePlayerInstant();

        // 시작 SFX (1회)
        if (qteStartSfx != null)
            AudioManager.Instance?.PlaySFX(qteStartSfx);

        // 루프 SFX 시작
        if (qteLoopSfx != null)
            AudioManager.Instance?.PlaySFXLoop(qteLoopSfx);

        // QTE Layer 활성화
        animator.SetLayerWeight(qteLayerIndex, 1f);

        // QTE 시작 상태 진입
        animator.SetInteger(_qteStateHash, 1);
    }

    /// <summary>
    /// QTE 종료 이벤트 수신
    /// - 여기서는 "결과 상태"만 설정한다
    /// - 결과 애니메이션은 QTE Layer에서 재생됨
    /// </summary>
    private void OnQTEEnded(QTEEndedEvent e)
    {
        if (e.Action != _myAction)
            return;

        // 루프 SFX 종료
        AudioManager.Instance?.StopSFXLoop();

        // 결과 상태 설정 (Animator가 전이를 결정)
        if (e.Result == QTEResult.Success)
            animator.SetInteger(_qteStateHash, 3); // Success
        else
            animator.SetInteger(_qteStateHash, 4); // Fail
    }

    // ======================================================
    // Animation Events (QTE Layer)
    // ======================================================

    /// <summary>
    /// QTE_Start 애니메이션 마지막 프레임에 연결
    /// - Start → Progress 전환
    /// </summary>
    public void OnPounceStartFinished()
    {
        animator.SetInteger(_qteStateHash, 2); // Progress
    }

    /// <summary>
    /// 죄수 피격 타이밍 프레임
    /// </summary>
    public void OnPrisonerHitFrame()
    {
        EventBus.Publish(new PrisonerHitTimingEvent());
    }

    /// <summary>
    /// 죄수 공격 타이밍 프레임
    /// </summary>
    public void OnPrisonerAttackFrame()
    {
        EventBus.Publish(new PlayerAttackTimingEvent());
    }

    /// <summary>
    /// 피격 SFX 재생용 애니메이션 이벤트
    /// </summary>
    public void OnPrisonerHitSfx()
    {
        if (hitSfx != null)
            AudioManager.Instance?.PlaySFX(hitSfx);
    }

    /// <summary>
    /// QTE 결과 애니메이션(Success / Fail) 마지막 프레임
    ///
    /// - QTE Layer 비활성화
    /// - QTEState 초기화
    /// - FSM 전환용 이벤트 발행
    /// </summary>
    public void OnQTEResultAnimationFinished()
    {
        // [수정 1] 순서 변경: FSM에게 먼저 알림 -> CombatState 진입 & IsCombat = true 설정됨
        if (_myAction != null)
        {
            EventBus.Publish(new QTEResultAnimationFinishedEvent
            {
                Action = _myAction
            });
        }

        // [수정 2] 그 다음 레이어 끄기: 이미 밑바닥(Base Layer)은 전투 자세를 취하고 있으므로 끊김 없음
        animator.SetLayerWeight(qteLayerIndex, 0f);
        animator.SetInteger(_qteStateHash, 0);

        _myAction = null;
    }

    /// <summary>
    /// 죄수의 애니메이션이 플레이어에게 향하도록 회전 값 보정
    /// </summary>

    private void FacePlayerInstant()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
            return;

        Vector3 dir = playerObj.transform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }
}





