using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseTrigger : MonoBehaviour
{
    private GamePhase targetPhase; // 전환될 목표 페이즈
   private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Briefing)
            {
                targetPhase = GamePhase.Patrol;
                Debug.Log("플레이어 감지. 순찰 페이즈로 전환합니다.");

                GameManager.Instance.ChangePhase(targetPhase);

                gameObject.SetActive(false);
            }
        }
    }
}
