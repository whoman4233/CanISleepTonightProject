using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkipHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject skipUIPanel;

    private bool _isDecisionMade = false; // 중복 입력 방지용 변수
    private bool _isInputEnabled = false; // 입력을 받아도 되는 상태인지

    private IEnumerator Start()
    {
        // 1. 초기 상태 설정
        if (skipUIPanel != null) skipUIPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. [핵심] 0.1~0.2초간 대기하여 로딩 씬의 잔상 입력을 무시
        // Time.timeScale이 0이어도 돌아가는 WaitForSecondsRealtime 사용
        yield return new WaitForSecondsRealtime(0.2f);

        // 3. 이제서야 시간 정지 및 입력 허용
        Time.timeScale = 0f;
        _isInputEnabled = true;
        Debug.Log("[Tutorial] Skip Input Enabled");
    }

    private void Update()
    {
        if (_isDecisionMade || !_isInputEnabled || FlowController.Instance.IsBusy) return;

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
            //Cursor.lockState = CursorLockMode.Locked;
            //Cursor.visible = false;
            skipUIPanel.SetActive(false);
            //EventBus.Publish(new GlobalInputLockRequestedEvent());
            EventBus.Publish(new OpenControlGuideEvent());
            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;
        }
    }
}
