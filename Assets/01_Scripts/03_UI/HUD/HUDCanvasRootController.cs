using UnityEngine;

public class HUDRootController : MonoBehaviour
{
    [Header("Patrol HUD Elements")]
    [SerializeField] private GameObject timerUI;
    [SerializeField] private GameObject crosshairUI;

    private void Awake()
    {
        SetPatrolHUD(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GamePhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GamePhaseChangedEvent>(OnPhaseChanged);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        bool showHUD =
            e.Phase == GamePhase.Briefing ||
            e.Phase == GamePhase.Patrol;

        SetPatrolHUD(showHUD);
    }

    private void SetPatrolHUD(bool active)
    {
        if (timerUI != null)
            timerUI.SetActive(active);

        if (crosshairUI != null)
            crosshairUI.SetActive(active);
    }
}

