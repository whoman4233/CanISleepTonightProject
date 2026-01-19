using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialSkipHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject skipUIPanel;

    private void Start()
    {
        if (skipUIPanel != null) skipUIPanel.SetActive(true);
    }

    private void Update()
    {
        // Q: 스킵
        if (Input.GetKeyDown(KeyCode.Q))
        {
            FlowController.Instance.EnterPlayFromTutorial();
        }
        // E: 진행
        else if (Input.GetKeyDown(KeyCode.E))
        {
            skipUIPanel.SetActive(false);
            Destroy(skipUIPanel);
        }
    }
}
