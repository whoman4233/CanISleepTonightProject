using System.Collections;
using UnityEngine;

public class PlaySceneBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 1. InputManager 준비 대기
        yield return new WaitUntil(() => InputManager.Instance != null);

        // 2. Player 준비 대기
        yield return new WaitUntil(() => FindObjectOfType<Player>() != null);

        var player = FindObjectOfType<Player>();

        // 3. Player 존재 알림 (입력 활성화 핵심)
        EventBus.Publish(new PlayerPresenceChangedEvent(true));

        // 4. InspectionManager 초기화 (있다면)
        var inspection = FindObjectOfType<InspectionManager>();
        if (inspection != null)
        {
            inspection.Initialize(InputManager.Instance.Inputs);
        }

        Debug.Log("[PlaySceneBootstrap] Player/Input 초기화 완료");
    }
}
