using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

[RequireComponent(typeof(Canvas))]
public class TutorialUICameraBinder : MonoBehaviour
{
    private Canvas _canvas;
    private Camera _uiCamera;

    private Action<PlayerSpawnedEvent> _onPlayerSpawned;

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _onPlayerSpawned = OnPlayerSpawned;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onPlayerSpawned);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPlayerSpawned);
    }

    private void Start()
    {
        var marker = FindObjectOfType<UICameraMarker>();
        if (marker == null)
        {
            Debug.LogError("[TutorialUICameraBinder] UICameraMarker 찾을 수 없음.");
            return;
        }

        _uiCamera = marker.GetComponent<Camera>();
        if (_uiCamera == null)
        {
            Debug.LogError("[TutorialUICameraBinder] Marker에 카메라 없음.");
            return;
        }

        // Canvas ← UIScene UICamera
        _canvas.renderMode = RenderMode.ScreenSpaceCamera;
        _canvas.worldCamera = _uiCamera;
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent e)
    {
        if (_uiCamera == null)
            return;

        var mainCamera = e.Player.GetComponentInChildren<Camera>(true);
        if (mainCamera == null)
            return;

        var mainData = mainCamera.GetUniversalAdditionalCameraData();
        if (mainData == null)
            return;

        if (!mainData.cameraStack.Contains(_uiCamera))
        {
            mainData.cameraStack.Add(_uiCamera);
        }
    }
}

