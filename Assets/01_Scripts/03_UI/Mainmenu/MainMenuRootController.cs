using System;
using UnityEngine;

public class MainMenuRootController : MonoBehaviour
{
    [SerializeField] private GameObject menuRoot;

    private Action<GamePhaseChangedEvent> _handler;

    private void Awake()
    {
        _handler = OnPhaseChanged;
        menuRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_handler);

        // 현재 Phase 즉시 반영 (타이밍 문제 해결)
        var gm = GameContext.Instance.Get<GameManager>();
        ApplyPhase(gm.CurrentPhase);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_handler);
    }

    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        ApplyPhase(e.Phase);
    }

    private void ApplyPhase(GamePhase phase)
    {
        menuRoot.SetActive(phase == GamePhase.NotStarted);
    }
}



