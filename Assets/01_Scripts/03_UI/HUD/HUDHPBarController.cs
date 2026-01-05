using UnityEngine;
using UnityEngine.UI;
using System;

public class UIHPBarController : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private HUDHeartAnimator heartAnimator;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fill Range")]
    [SerializeField] private float fillMin = 0.09f; // HP 0
    [SerializeField] private float fillMax = 0.9f;  // HP 100

    private const float MaxHp = 100f;
    private GamePhase _currentPhase;

    private Action<PlayerHpChangedEvent> _onHpChanged;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _onHpChanged = OnHpChanged;
        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onHpChanged);
        EventBus.Subscribe(_onPhaseChanged);

        if (GameManager.Instance != null)
            _currentPhase = GameManager.Instance.CurrentPhase;

        RefreshVisibility();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onHpChanged);
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        _currentPhase = e.Phase;
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool show =
            _currentPhase == GamePhase.Standby ||
            _currentPhase == GamePhase.Briefing ||
            _currentPhase == GamePhase.Patrol;

        canvasGroup.alpha = show ? 1f : 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnHpChanged(PlayerHpChangedEvent e)
    {
        ApplyHp(e.CurrentHp);
    }

    private void ApplyHp(int hp)
    {
        float normalized = Mathf.Clamp01(hp / MaxHp);

        // FillAmount 범위 고정
        float fillAmount = Mathf.Lerp(fillMin, fillMax, normalized);
        fillImage.fillAmount = fillAmount;

        // 컬러 변화
        fillImage.color = Color.Lerp(Color.red, Color.white, normalized);

        if (heartAnimator != null)
            heartAnimator.UpdateByHp(normalized);
    }
}

