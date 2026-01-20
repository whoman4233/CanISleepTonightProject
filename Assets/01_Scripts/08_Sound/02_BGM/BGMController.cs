using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMController : MonoBehaviour
{
    [SerializeField] private BGMDatabase database;

    private BGMData _current;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 첫 씬 진입 대응
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var next = database.GetByScene(scene.name);
        if (next == null)
            return;

        if (_current == next)
            return;

        Play(next);
        _current = next;
    }

    private void Play(BGMData data)
    {
        AudioManager.Instance.PlayBGM(data.clip, data.loop);
    }
}

