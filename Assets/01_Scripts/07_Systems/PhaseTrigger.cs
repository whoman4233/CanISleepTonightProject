using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseTrigger : MonoBehaviour
{
    private GamePhase targetPhase; // 전환될 목표 페이즈
   private string playerTag = "Player";
    private bool _triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        var phase = GameManager.Instance.CurrentPhase;

        // ★ [핵심] Standby 또는 Briefing 모두 허용
        if (phase == GamePhase.Briefing || phase == GamePhase.Standby)
        {
            _triggered = true;

            Debug.Log($"PhaseTrigger: {phase} → Patrol 전환");
            GameManager.Instance.ChangePhase(GamePhase.Patrol);
        }
    }
}

