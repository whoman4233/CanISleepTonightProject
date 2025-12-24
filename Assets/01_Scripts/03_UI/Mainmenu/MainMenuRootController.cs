using UnityEngine;

public class MainMenuRootController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;

    private void Awake()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);
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
        bool isMenuPhase = e.Phase == GamePhase.NotStarted;

        if (menuRoot != null)
            menuRoot.SetActive(isMenuPhase);
    }
}
