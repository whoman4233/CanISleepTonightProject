using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeBroadcaster : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 로드되었을 때, 실제 ActiveScene 기준으로 알림
        var activeScene = SceneManager.GetActiveScene();
        EventBus.Publish(new SceneChangedEvent(activeScene.name, mode));
    }
}
