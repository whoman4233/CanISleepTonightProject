using System;
using UnityEngine;

public class QTEPresenter : MonoBehaviour
{
    private QTEController _controller;
    private string _currentQTEId;
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
        Debug.Log("[QTEPresenter] OnQTEStarted");
        // 이미 QTE 진행 중이면 무시
        if (_controller != null)
            return;

        // QTE UI 표시
        if (qteRoot != null)
            qteRoot.SetActive(true);
        Debug.Log("[QTEPresenter] Root Activated");
        // 컨텍스트 저장
        _currentQTEId = e.QTEId;

        // 순수 로직 컨트롤러 생성
        _controller = new QTEController(e.Config);

        // QTE 전용 입력 리더 생성
        _inputReader = new QTEInputReader(InputManager.Instance.Inputs, _controller);
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        // 외부 시스템용 QTE 종료 이벤트 재발행
        if (!string.IsNullOrEmpty(_currentQTEId))
        {
            EventBus.Publish(new QTEEndedEvent
            {
                QTEId = _currentQTEId,
                Result = e.Result
            });
        }

        // 입력 해제
        _inputReader?.Dispose();
        _inputReader = null;

        // 로직 정리
        _controller = null;
        _currentQTEId = null;

        // QTE UI 숨김
        if (qteRoot != null)
            qteRoot.SetActive(false);
    }
}



