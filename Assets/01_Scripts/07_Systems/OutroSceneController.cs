using UnityEngine;
using UnityEngine.Playables;

public class OutroSceneController : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;

    private void Awake()
    {
        if (director == null)
            director = GetComponentInChildren<PlayableDirector>();
    }

    private void OnEnable()
    {
        if (director != null)
            director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (director != null)
            director.stopped -= OnTimelineStopped;
    }

    private void Start()
    {
        director?.Play();
    }

    private void OnTimelineStopped(PlayableDirector d)
    {
        Debug.Log("[Outro] Timeline Finished (PlayableDirector)");
        EventBus.Publish(new OutroFinishedEvent());
    }
}
