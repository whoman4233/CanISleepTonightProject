using UnityEngine;
using System;

public class MainMenuRootController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private MainMenuController menuController;

    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        if (menuRoot != null)
            menuRoot.SetActive(false);

        _onPhaseChanged = OnPhaseChanged;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);

        // 현재 페이즈 즉시 반영 (DDOL / 씬 복귀 대응)
        if (GameManager.Instance != null)
        {
            OnPhaseChanged(new GamePhaseChangedEvent(GameManager.Instance.CurrentPhase));
        }
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        bool isMenuPhase = e.Phase == GamePhase.NotStarted;

        if (menuRoot != null)
            menuRoot.SetActive(isMenuPhase);

        // 메뉴 페이즈 진입 시 내부 상태 초기화
        if (isMenuPhase && menuController != null)
        {
            menuController.ResetState();
        }
    }
}
