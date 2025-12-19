using UnityEditor;
using UnityEngine;

public class UIStateManager : MonoBehaviour
{
    [Header("Phase UI")]
    [SerializeField] private GameObject menuUI;          // NotStarted
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
        // Phase UI 초기화
        menuUI.SetActive(false);
        resultUI.SetActive(false);

        // Phase UI 선택
        switch (phase)
        {
            case GamePhase.NotStarted:
                menuUI.SetActive(true);
                break;

            case GamePhase.Settlement:
            case GamePhase.OffDuty:
            case GamePhase.Ending:
                resultUI.SetActive(true);
                break;
        }

        // Gameplay Overlay
        bool isGameplay =
            phase == GamePhase.Briefing ||
            phase == GamePhase.Patrol;

        hudUI.SetActive(isGameplay);

        // InGameMenu / Popup은 대부분 허용
        inGameMenuUI.SetActive(phase != GamePhase.NotStarted);
        popupUI.SetActive(true); // 필요시 조건 추가
    }

}
