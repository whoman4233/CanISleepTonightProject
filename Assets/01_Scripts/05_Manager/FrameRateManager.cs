using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    [SerializeField] private int targetFPS = 90;

    void Awake()
    {
        // 1. 수직 동기화를 꺼야 targetFrameRate가 제대로 작동합니다.
        QualitySettings.vSyncCount = 0;

        // 2. 원하는 프레임 수치로 고정합니다.
        Application.targetFrameRate = targetFPS;

        Debug.Log($"프레임이 {targetFPS}로 설정되었습니다.");
    }
}