using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class HUDHPBarController : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("HP Gauge")]
    [SerializeField] private Image fillImage;
    [SerializeField] private HUDHeartAnimator heartAnimator;

    [Header("Fill Range")]
    [SerializeField] private float fillMin = 0.1f;
    [SerializeField] private float fillMax = 1.0f;

    [Header("HP Text")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private Color highHpColor = Color.green;
    [SerializeField] private Color midHpColor = Color.yellow;
    [SerializeField] private Color lowHpColor = Color.red;

    [Header("HP Warning SFX")]
    [SerializeField] private AudioClip hpDangerClip; // HP <= 30
    [SerializeField] private AudioClip hpZeroClip;   // HP == 0

    [SerializeField] private float maxHp = 100f;
    [SerializeField] private int dangerThreshold = 30;

    // SFX 중복 방지 플래그
    private bool _dangerTriggered;
    private bool _zeroTriggered;

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

        // 루프/씬 초기화 시 SFX 완전 정리
        _onContextReady = _ =>
        {
            if (GameManager.Instance != null)
                _currentPhase = GameManager.Instance.CurrentPhase;

            AudioManager.Instance.StopSFXLoop();
            ResetWarningFlags();

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

        ResetWarningFlags();
        ForceRefreshVisibility();
    }

    private void OnDisable()
    {
        // HUD 비활성화 시 루프 사운드 보장 중단
        AudioManager.Instance.StopSFXLoop();

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
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "01_IntroScene" || sceneName == "03_OutroScene")
        {
            if (root != null)
                root.SetActive(false);
            return;
        }

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
        float normalized = Mathf.Clamp01(hp / maxHp);

        fillImage.fillAmount = normalized;

        fillImage.color = Color.Lerp(Color.red, Color.white, normalized);

        if (heartAnimator != null)
            heartAnimator.UpdateByHp(normalized);

        // =========================
        // HP 숫자 텍스트
        // =========================
        if (hpText != null)
        {
            hpText.text = Mathf.CeilToInt(hp).ToString();
            hpText.color = EvaluateHpColor(normalized);
        }

        // HP 경고 사운드 처리
        HandleHpWarningSfx(hp);
    }

    // =========================
    // HP 비율에 따른 색상 계산
    // =========================
    private Color EvaluateHpColor(float normalized)
    {
        if (normalized >= 0.5f)
        {
            float t = (normalized - 0.5f) / 0.5f;
            return Color.Lerp(midHpColor, highHpColor, t);
        }
        else
        {
            float t = normalized / 0.5f;
            return Color.Lerp(lowHpColor, midHpColor, t);
        }
    }

    // =========================
    // HP 경고 SFX 처리
    // =========================
    private void HandleHpWarningSfx(int hp)
    {
        // HP == 0
        if (hp <= 0)
        {
            if (!_zeroTriggered && hpZeroClip != null)
            {
                AudioManager.Instance.StopSFXLoop(); // 루프 정리
                AudioManager.Instance.PlaySFX(hpZeroClip);
                _zeroTriggered = true;
            }
            return;
        }

        // HP <= DangerThreshold
        if (hp <= dangerThreshold)
        {
            if (!_dangerTriggered && hpDangerClip != null)
            {
                AudioManager.Instance.PlaySFXLoop(hpDangerClip);
                _dangerTriggered = true;
            }
        }
        else
        {
            // 회복 시 루프 사운드 반드시 중단
            if (_dangerTriggered)
            {
                AudioManager.Instance.StopSFXLoop();
            }

            _dangerTriggered = false;
            _zeroTriggered = false;
        }
    }

    // =========================
    // 경고 상태 리셋
    // =========================
    private void ResetWarningFlags()
    {
        _dangerTriggered = false;
        _zeroTriggered = false;
    }
}


