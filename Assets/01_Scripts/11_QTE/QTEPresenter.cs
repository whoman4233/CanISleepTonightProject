using System;
using UnityEngine;

public class QTEPresenter : MonoBehaviour
{
    private QTEController _controller;
    private QTEInputReader _inputReader;

    [Header("QTE UI Root")]
    [SerializeField] private GameObject qteRoot;

    private Action<QTEStartedEvent> _onQTEStarted;
    private Action<QTEEndedEvent> _onQTEEnded;

    private void Awake()
    {
        _onQTEStarted = OnQTEStarted;
        _onQTEEnded = OnQTEEnded;

        if (qteRoot != null)
            qteRoot.SetActive(false);
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
        // 상세보기 강제 종료
        EventBus.Publish(new ForceExitInspectionEvent());

        // 이미 QTE 중이면 무시
        if (_controller != null)
            return;

        Debug.Log($"[QTEPresenter] Start QTE : {e.Action.name}");

        if (qteRoot != null)
            qteRoot.SetActive(true);

        _controller = new QTEController(e.Action);

        _inputReader = new QTEInputReader(
            InputManager.Instance.Inputs,
            _controller
        );
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        Debug.Log($"[QTEPresenter] End QTE : {e.Action.name} / {e.Result}");

        _inputReader?.Dispose();
        _inputReader = null;

        _controller = null;

        if (qteRoot != null)
            qteRoot.SetActive(false);
    }
}





