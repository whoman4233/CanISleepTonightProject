using System;
using UnityEngine;

public class HiddenItemDiscoverySfxController : MonoBehaviour
{
    [SerializeField] private AudioClip discoverySfx;

    private Action<HiddenItemFoundEvent> _onFound;

    private void Awake()
    {
        _onFound = OnItemFound;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onFound);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onFound);
    }

    private void OnItemFound(HiddenItemFoundEvent e)
    {
        if (discoverySfx == null)
            return;

        AudioManager.Instance.PlaySFX(discoverySfx);
    }
}
