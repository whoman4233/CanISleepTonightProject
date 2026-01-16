using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDHPBarController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [SerializeField] private Image fillImage;
    [SerializeField] private HUDHeartAnimator heartAnimator;

    [Header("Fill Range")]
    [SerializeField] private float fillMin = 0.09f;
    [SerializeField] private float fillMax = 0.9f;

    private const float MaxHp = 100f;
    private GamePhase _currentPhase;

    private Action<PlayerHpChangedEvent> _onHpChanged;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;
    private Action<GameContextReadyEvent> _onContextReady;

    private void Awake()
    {
        _onHpChanged = OnHpChanged;
        _onPhaseChanged = e =>
        {
            _currentPhase = e.Phase;
            ForceRefreshVisibility();
        };

        _onContextReady = _ =>
        {
            if (GameManager.Instance != null)
                _currentPhase = GameManager.Instance.CurrentPhase;

            ForceRefreshVisibility();
        };
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onHpChanged);
        EventBus.Subscribe(_onPhaseChanged);
        EventBus.Subscribe(_onContextReady);

        if (GameManager.Instance != null)
        {
            _currentPhase = GameManager.Instance.CurrentPhase;
            ApplyHp(GameManager.Instance.PlayerHP); // 초기 강제 반영
        }

        ForceRefreshVisibility();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onHpChanged);
        EventBus.Unsubscribe(_onPhaseChanged);
        EventBus.Unsubscribe(_onContextReady);
    }

    private void ForceRefreshVisibility()
    {
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        bool show =
         _currentPhase == GamePhase.Tutorial ||
         _currentPhase == GamePhase.Standby ||
         _currentPhase == GamePhase.Briefing ||
         _currentPhase == GamePhase.Patrol;

        if (root != null)
            root.SetActive(show);

        if (!show && heartAnimator != null)
            heartAnimator.StopBeat();
    }

    private void OnHpChanged(PlayerHpChangedEvent e)
    {
        ApplyHp(e.CurrentHp);
    }

    private void ApplyHp(int hp)
    {
        float normalized = Mathf.Clamp01(hp / MaxHp);
        float fillAmount = Mathf.Lerp(fillMin, fillMax, normalized);

        fillImage.fillAmount = fillAmount;
        fillImage.color = Color.Lerp(Color.red, Color.white, normalized);

        if (heartAnimator != null)
            heartAnimator.UpdateByHp(normalized);
    }
}

