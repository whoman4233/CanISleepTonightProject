using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    [Header("Phase Roots")]
    [SerializeField] private GameObject mainMenuRoot; //메인 메뉴
    [SerializeField] private GameObject briefingRoot; // 브리핑관련
    [SerializeField] private GameObject hudRoot; //HUD 
    [SerializeField] private GameObject settlementRoot; //정산
    [SerializeField] private GameObject offDutyRoot; // 퇴근
    [SerializeField] private GameObject endingRoot; // 엔딩

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
        mainMenuRoot.SetActive(phase == GamePhase.NotStarted);
        briefingRoot.SetActive(phase == GamePhase.Briefing);
        hudRoot.SetActive(phase == GamePhase.Standby || phase == GamePhase.Patrol);
        settlementRoot.SetActive(phase == GamePhase.Settlement);
        offDutyRoot.SetActive(phase == GamePhase.OffDuty);
        endingRoot.SetActive(phase == GamePhase.Ending);
    }
}
