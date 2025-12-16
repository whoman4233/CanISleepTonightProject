using UnityEngine;

public class MenuCanvasRootController : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<ShowMainMenuEvent>(_ => Show());
        EventBus.Subscribe<HideMainMenuEvent>(_ => Hide());
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<ShowMainMenuEvent>(_ => Show());
        EventBus.Unsubscribe<HideMainMenuEvent>(_ => Hide());
    }

    private void Show() => gameObject.SetActive(true);
    private void Hide() => gameObject.SetActive(false);
}
