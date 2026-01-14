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
        EventBus.Publish(new SceneChangedEvent(scene.name, mode));
    }
}
