using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class BindUICameraToMainCamera : MonoBehaviour
{
    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        // DDOL에 존재하는 UI Camera 찾기
        var uiMarker = FindObjectOfType<UICameraMarker>();
        if (uiMarker == null)
        {
            Debug.LogWarning("[BindUICamera] UICameraMarker not found.");
            return;
        }

        Camera uiCamera = uiMarker.GetComponent<Camera>();
        if (uiCamera == null)
            return;

        // 이 Camera = MainCamera (Base)
        var baseCamera = GetComponent<Camera>();
        var data = baseCamera.GetUniversalAdditionalCameraData();

        if (data.renderType != CameraRenderType.Base)
        {
            Debug.LogWarning("[BindUICamera] Base Camera가 아님");
            return;
        }

        // 중복 방지
        if (!data.cameraStack.Contains(uiCamera))
        {
            data.cameraStack.Add(uiCamera);
        }
    }
}
