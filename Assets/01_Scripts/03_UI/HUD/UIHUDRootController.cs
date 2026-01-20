using System;
using UnityEngine;

public class UIHUDRootController : MonoBehaviour
{
    [SerializeField] private GameObject root;

    private bool _isLoading;

    private Action<LoadingOverlayShownEvent> _onLoadingShown;
    private Action<LoadingOverlayHiddenEvent> _onLoadingHidden;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        _onLoadingShown = OnLoadingShown;
        _onLoadingHidden = OnLoadingHidden;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onLoadingShown);
        EventBus.Subscribe(_onLoadingHidden);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onLoadingShown);
        EventBus.Unsubscribe(_onLoadingHidden);
    }

    private void OnLoadingShown(LoadingOverlayShownEvent e)
    {
        _isLoading = true;
        Apply();
    }

    private void OnLoadingHidden(LoadingOverlayHiddenEvent e)
    {
        _isLoading = false;
        Apply();
    }

    private void Apply()
    {
        if (root != null)
            root.SetActive(!_isLoading);
    }
}
