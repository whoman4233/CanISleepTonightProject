using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    [Header("Result UI")]
    [SerializeField] private GameObject resultUI;        // Settlement / OffDuty / Ending

    [Header("Gameplay Overlay")]
    [SerializeField] private GameObject hudUI;           // Briefing / Patrol
    [SerializeField] private GameObject inGameMenuUI;    // 대부분의 Phase
    [SerializeField] private GameObject popupUI;         // 공통
    [SerializeField] private GameObject inspectionUI;    // 공통 (조건부)

    private System.Action<GamePhaseChangedEvent> phaseHandler;

    private void Awake()
    {
        phaseHandler = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(phaseHandler);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(phaseHandler);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        ApplyPhase(e.Phase);
    }

    private void ApplyPhase(GamePhase phase)
    {
        // Result UI
        bool isResult =
            phase == GamePhase.Settlement ||
            phase == GamePhase.OffDuty ||
            phase == GamePhase.Ending;

        resultUI.SetActive(isResult);

        // Gameplay Overlay
        bool isGameplay =
            phase == GamePhase.Briefing ||
            phase == GamePhase.Patrol;

        hudUI.SetActive(isGameplay);

        // InGameMenu / Popup
        inGameMenuUI.SetActive(phase != GamePhase.NotStarted);
        popupUI.SetActive(true);
    }
}

