using System;
using UnityEngine;

public class QTEPresenter : MonoBehaviour
{
    private QTEController _controller;
    private string _currentQTEId;
    private QTEInputReader _inputReader;

    // EventBus handlers (강한 참조)
    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    private void Awake()
    {
        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;
    }

    private void OnEnable()
    {
        EventBus.Subscribe(_onQTEStarted);
        EventBus.Subscribe(_onQTEEnded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe(_onQTEStarted);
        EventBus.Unsubscribe(_onQTEEnded);
    }

    private void Update()
    {
        _controller?.Tick(Time.deltaTime);
    }

    private void OnQTEStarted(QTEStartedEvent e)
    {
        if (_controller != null)
            return;

        _currentQTEId = e.QTEId;
        _controller = new QTEController(e.Config);
        _inputReader = new QTEInputReader(InputManager.Instance.Inputs, _controller);

    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        // Controller가 보낸 이벤트( QTEId 없음 )를
        // Presenter가 "완성된 이벤트"로 재발행
        if (!string.IsNullOrEmpty(_currentQTEId))
        {
            EventBus.Publish(new QTEEndedEvent
            {
                QTEId = _currentQTEId,
                Result = e.Result
            });
        }

        _inputReader?.Dispose();
        _inputReader = null;

        _controller = null;
        _currentQTEId = null;
    }
}



