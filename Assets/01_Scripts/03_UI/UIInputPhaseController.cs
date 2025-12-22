using UnityEngine;
using UnityEngine.InputSystem;

public class UIInputPhaseController : MonoBehaviour
{
    [SerializeField]
    private InputActionAsset uiActions; // EventSystem에 연결된 것과 동일한 에셋

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
        if (e.Phase == GamePhase.NotStarted)
        {
            uiActions.FindActionMap("UI", true).Enable();

            var playerMap = uiActions.FindActionMap("Player", false);
            if (playerMap != null) playerMap.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void LateUpdate()
    {
        if (GameManager.Instance.CurrentPhase == GamePhase.NotStarted)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}