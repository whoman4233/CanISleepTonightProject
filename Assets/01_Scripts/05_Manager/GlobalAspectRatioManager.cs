using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalAspectRatioManager : MonoBehaviour
{
    private static GlobalAspectRatioManager instance;
    private float targetAspect = 16f / 9f;

    void Awake()
    {
        // 1. 싱글톤 설정: 단 하나만 존재하게 함
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        // 2. 씬이 로드될 때마다 실행될 함수 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 씬이 바뀌면 자동으로 호출됨
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToAllCameras();
    }

    // 카메라 상태가 수시로 변하는 게임이라면 LateUpdate에서 지속적으로 체크
    void LateUpdate()
    {
        ApplyToAllCameras();
    }

    public void ApplyToAllCameras()
    {
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        foreach (Camera cam in Camera.allCameras)
        {
            // 배경 카메라는 비율 조절 제외
            if (cam.gameObject.name == "BackgroundCamera") continue;

            Rect rect = cam.rect;
            if (scaleHeight < 1.0f)
            {
                rect.width = 1.0f;
                rect.height = scaleHeight;
                rect.x = 0;
                rect.y = (1.0f - scaleHeight) / 2.0f;
            }
            else
            {
                float scaleWidth = 1.0f / scaleHeight;
                rect.width = scaleWidth;
                rect.height = 1.0f;
                rect.x = (1.0f - scaleWidth) / 2.0f;
                rect.y = 0;
            }
            cam.rect = rect;
        }
    }
}