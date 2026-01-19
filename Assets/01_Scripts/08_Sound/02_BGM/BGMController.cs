using System;
using UnityEngine;

public class BGMController : MonoBehaviour
{
    [SerializeField] private BGMDatabase database;

    private BGMData _current;
    private Action<GamePhaseChangedEvent> _onPhaseChanged;

    private void Awake()
    {
        _onPhaseChanged = OnPhaseChanged;
    }
    private void Start()
    {
        PreloadAllBGM();
    }
    private void OnEnable()
    {
        EventBus.Subscribe(_onPhaseChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onPhaseChanged);
    }
    private void PreloadAllBGM() // * 비동기 로딩을 통한 렉유발 해결
    {
        foreach (var bgm in database.All)
        {
            if (bgm.clip != null)
            {
                bgm.clip.LoadAudioData();
            }
        }
    }
    private void OnPhaseChanged(GamePhaseChangedEvent e)
    {
        var next = database.Get(e.Phase);
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

        Debug.Log($"[BGM] 변경 페이즈-> {data.phase}");
    }
}
