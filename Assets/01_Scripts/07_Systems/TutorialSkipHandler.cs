using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkipHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject skipUIPanel;

    private bool _isDecisionMade = false; // 중복 입력 방지용 변수

    private void Start()
    {
        // 시간 정지 및 커서 해제
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (skipUIPanel != null) skipUIPanel.SetActive(true);
    }

    private void Update()
    {
        if (_isDecisionMade) return;
        // Q: 스킵
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _isDecisionMade = true;
            Time.timeScale = 1f; // 시간 복구
            FlowController.Instance.EnterPlayFromTutorial();
        }
        // E: 진행
        else if (Input.GetKeyDown(KeyCode.E))
        {
            _isDecisionMade = true;
            Time.timeScale = 1f; // 시간 복구
            // 커서 다시 잠금
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            skipUIPanel.SetActive(false);
            Destroy(gameObject);
        }
    }
}
