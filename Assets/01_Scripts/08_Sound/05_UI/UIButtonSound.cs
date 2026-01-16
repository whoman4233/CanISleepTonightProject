using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    [SerializeField] private AudioClip clickClip;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlaySound);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(PlaySound);
    }

    private void PlaySound()
    {
        if (clickClip == null)
            return;

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayUISound(clickClip);
    }
}
