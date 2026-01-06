using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntoPlayScene : MonoBehaviour
{
    private string playerTag = "Player";

    private void OnTriggerEnter(Collider other) // 튜토리얼씬 디버그용 플레이씬 진입 루트 확보
    {
        if (other.CompareTag(playerTag))
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Tutorial)
            {
                FlowController.Instance.EnterPlayFromTutorial();
            }
        }
    }

}
