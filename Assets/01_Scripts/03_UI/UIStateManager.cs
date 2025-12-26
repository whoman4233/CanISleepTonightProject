using UnityEngine;
using System;

public class UIStateManager : MonoBehaviour
{
    [Header("Gameplay Overlay")]
    [SerializeField] private GameObject hudUI;           // Briefing / Patrol
    [SerializeField] private GameObject inGameMenuUI;    // 대부분의 Phase
    [SerializeField] private GameObject popupUI;         // 공통
    [SerializeField] private GameObject inspectionUI;    // 공통 (조건부)

    private Action<GamePhaseChangedEvent> phaseHandler;

    private void Awake()
    {
        phaseHandler = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(phaseHandler);

        if (GameManager.Instance != null)
            ApplyPhase(GameManager.Instance.CurrentPhase);
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
        // =========================
        // Gameplay Overlay
        // =========================

        bool isGameplay =
            phase == GamePhase.Briefing ||
            phase == GamePhase.Standby ||
            phase == GamePhase.Patrol;

        hudUI.SetActive(isGameplay);

        // =========================
        // InGameMenu / Popup
        // =========================

        inGameMenuUI.SetActive(phase != GamePhase.NotStarted);
        popupUI.SetActive(true);

        // Result UI는 여기서 절대 제어하지 않는다
    }
}


