using System;
using UnityEngine;
using UnityEngine.UI;

public class UIHPBarController : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private HUDHeartAnimator heartAnimator;

    private const float MaxHp = 100f;

    private Action<PlayerHpChangedEvent> _onHpChanged;

    private void Awake()
    {
        _onHpChanged = OnHpChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onHpChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onHpChanged);
    }

    private void OnHpChanged(PlayerHpChangedEvent e)
    {
        ApplyHp(e.CurrentHp);
    }

    private void ApplyHp(int hp)
    {
        float normalized = Mathf.Clamp01(hp / MaxHp);

        // Fill
        fillImage.fillAmount = normalized;

        // Color (White → Red)
        fillImage.color = Color.Lerp(Color.red, Color.white, normalized);

        // Heart animation
        if (heartAnimator != null)
            heartAnimator.UpdateByHp(normalized);
    }
}
