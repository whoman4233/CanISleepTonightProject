using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class Crosshair : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root; //실제 표시 제어용 Root

    [Header("UI")]
    [SerializeField] private RectTransform crosshair;
    [SerializeField] private Image image;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Color")]
    [SerializeField] private Color normalColor = new Color(1, 1, 1, 0.4f);
    [SerializeField] private Color interactColor = new Color(1, 1, 0, 1f);

    [Header("Scale")]
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float interactScale = 2.0f;

    [Header("Tween")]
    [SerializeField] private float tweenDuration = 0.15f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private Tween _scaleTween;
    private Tween _colorTween;

    private bool _visible;

    // =========================
    // 상태 캐시
    // =========================
    private GamePhase _currentPhase = GamePhase.NotStarted;
    private bool _playerPresent;
    private bool _inspectionActive;

    // =========================
    // Events
    // =========================
    private Action<InteractableHoverChangedEvent> _onHover;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<PlayerPresenceChangedEvent> _onPlayerPresenceChanged;
    private Action<InspectionStartedEvent> _onInspectionStart;
    private Action<InspectionEndedEvent> _onInspectionEnd;
    private Action<GameContextReadyEvent> _onContextReady;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        ApplyVisible(false);

        // =========================
        // Hover 연출 (기존 로직 유지)
        // =========================
        _onHover = e =>
        {
            if (!_visible)
                return;

            SetInteractable(e.IsHovering);
        };

        // =========================
        // Phase 변경
        // =========================
        _onPhaseChanged = e =>
        {
            _currentPhase = e.Phase;
            RefreshVisibility();
        };

        // =========================
        // Player Presence
        // =========================
        _onPlayerPresenceChanged = e =>
        {
            _playerPresent = e.IsPresent;
            RefreshVisibility();
        };

        // =========================
        // Inspection 상태
        // =========================
        _onInspectionStart = _ =>
        {
            _inspectionActive = true;
            RefreshVisibility();
        };

        _onInspectionEnd = _ =>
        {
            _inspectionActive = false;
            RefreshVisibility();
        };

        // =========================
        // Context Ready (루프/씬 초기화)
        // =========================
        _onContextReady = _ =>
        {
            // Root 직접 제어 금지, 상태만 리셋
            _inspectionActive = false;
            _playerPresent = false;

            if (GameManager.Instance != null)
                _currentPhase = GameManager.Instance.CurrentPhase;

            RefreshVisibility(); // 강제 재판정
        };
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onHover);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onPlayerPresenceChanged);
        EventBus.Subscribe(_onInspectionStart);
        EventBus.Subscribe(_onInspectionEnd);
        EventBus.Subscribe(_onContextReady); // 누락돼 있던 부분

        if (GameManager.Instance != null)
            _currentPhase = GameManager.Instance.CurrentPhase;

        // Player 존재는 이벤트가 오기 전까지 false일 수 있음
        if (InputManager.Instance != null &&
            InputManager.Instance.CurrentState != InputState.UIOnly)
        {
            _playerPresent = true;
        }

        RefreshVisibility(); // 이벤트 기다리지 않고 즉시 동기화
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onHover);
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onPlayerPresenceChanged);
        EventBus.Unsubscribe(_onInspectionStart);
        EventBus.Unsubscribe(_onInspectionEnd);
        EventBus.Unsubscribe(_onContextReady);
    }

    // =========================
    // Visibility 판단
    // =========================
    private void RefreshVisibility()
    {
        // Outro / Intro Scene에서는 무조건 숨김
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "01_IntroScene" || sceneName == "03_OutroScene")
        {
            ApplyVisible(false);
            return;
        }

        bool uiOnly =
            InputManager.Instance != null &&
            InputManager.Instance.CurrentState == InputState.UIOnly;

        // 브리핑 페이즈는 UIOnly라도 Crosshair 허용
        bool allowInUiOnlyPhase =
            _currentPhase == GamePhase.Briefing;

        if (uiOnly && !allowInUiOnlyPhase)
        {
            ApplyVisible(false);
            return;
        }

        if (_inspectionActive)
        {
            ApplyVisible(false);
            return;
        }

        bool phaseOk =
            _currentPhase == GamePhase.Tutorial ||
            _currentPhase == GamePhase.Briefing ||
            _currentPhase == GamePhase.Standby ||
            _currentPhase == GamePhase.Patrol;

        bool show =
            phaseOk &&
            (_currentPhase == GamePhase.Briefing || _playerPresent);

        ApplyVisible(show);
    }

    // =========================
    // 실제 표시 적용
    // =========================
    private void ApplyVisible(bool show)
    {
        _visible = show;

        if (root != null)
            root.SetActive(show);

        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        if (!show)
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();

            // 숨김 시 반드시 기본 상태로 복귀
            crosshair.localScale = Vector3.one * normalScale;
            image.color = normalColor;
        }
        else
        {
            crosshair.localScale = Vector3.one * normalScale;
            image.color = normalColor;
        }
    }

    // =========================
    // Hover 연출
    // =========================
    private void SetInteractable(bool interactable)
    {
        _scaleTween?.Kill();
        _colorTween?.Kill();

        _scaleTween = crosshair
            .DOScale(interactable ? interactScale : normalScale, tweenDuration)
            .SetEase(ease);

        _colorTween = image
            .DOColor(interactable ? interactColor : normalColor, tweenDuration);
    }
}
