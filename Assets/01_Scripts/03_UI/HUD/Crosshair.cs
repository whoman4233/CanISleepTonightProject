using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class Crosshair : MonoBehaviour
{
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

    // 상태 캐시 
    private GamePhase _currentPhase = GamePhase.NotStarted;
    private bool _playerPresent;
    private bool _inspectionActive;

    private Action<InteractableHoverChangedEvent> _onHover;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;                 
    private Action<PlayerPresenceChangedEvent> _onPlayerPresenceChanged;   
    private Action<InspectionStartedEvent> _onInspectionStart;
    private Action<InspectionEndedEvent> _onInspectionEnd;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        ApplyVisible(false);

        _onHover = e =>
        {
            if (!_visible)
                return;

            SetInteractable(e.IsHovering);
        };

        //  페이즈 변경을 이벤트로 추적
        _onPhaseChanged = e =>
        {
            _currentPhase = e.Phase;
            RefreshVisibility();
        };

        //  Player 생성 타이밍(Standby 생성 등) 대응
        _onPlayerPresenceChanged = e =>
        {
            _playerPresent = e.IsPresent;
            RefreshVisibility();
        };

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
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onHover);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onPlayerPresenceChanged);
        EventBus.Subscribe(_onInspectionStart);
        EventBus.Subscribe(_onInspectionEnd);

        
        if (GameManager.Instance != null)
            _currentPhase = GameManager.Instance.CurrentPhase;

        // Player 존재는 이벤트가 오기 전까지 false일 수 있음.
        // InputManager state를 통해 추론 가능
        if (InputManager.Instance != null && InputManager.Instance.CurrentState != InputState.UIOnly)
            _playerPresent = true;

        RefreshVisibility();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onHover);
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onPlayerPresenceChanged);
        EventBus.Unsubscribe(_onInspectionStart);
        EventBus.Unsubscribe(_onInspectionEnd);
    }

    private void RefreshVisibility()
    {
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
            _currentPhase == GamePhase.Briefing ||
            _currentPhase == GamePhase.Standby ||
            _currentPhase == GamePhase.Patrol;

        bool show =
            phaseOk &&
            (_currentPhase == GamePhase.Briefing || _playerPresent);

        ApplyVisible(show);
    }


    private void ApplyVisible(bool show)
    {
        _visible = show;

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






