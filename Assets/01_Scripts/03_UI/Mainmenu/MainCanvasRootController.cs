using System;
using UnityEngine;

public class MenuCanvasRootController : MonoBehaviour
{
    private Action<ShowMainMenuEvent> _onShow;
    private Action<HideMainMenuEvent> _onHide;

    private void Awake()
    {
        gameObject.SetActive(false);

        _onShow = OnShow;
        _onHide = OnHide;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onShow);
        EventBus.Subscribe(_onHide);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onShow);
        EventBus.Unsubscribe(_onHide);
    }

    private void OnShow(ShowMainMenuEvent e)
    {
        gameObject.SetActive(true);
    }

    private void OnHide(HideMainMenuEvent e)
    {
        gameObject.SetActive(false);
    }
}
