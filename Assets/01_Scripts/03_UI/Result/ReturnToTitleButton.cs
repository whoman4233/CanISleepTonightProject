using UnityEngine;
using UnityEngine.UI;

public class ReturnToTitleButton : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        EventBus.Publish(new ResumeGameRequestedEvent());
        EventBus.Publish(new ReturnToTitleRequestedEvent());
    }
}
