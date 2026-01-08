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
        // 상세보기 강제 종료 요청
        EventBus.Publish(new ForceExitInspectionEvent());

        // 이미 진행 중이면 무시
        if (_controller != null)
            return;

        Debug.Log("[QTEPresenter] OnQTEStarted");

        // UI 표시
        if (qteRoot != null)
            qteRoot.SetActive(true);

        // QTEController 생성 (QTEId 전달)
        _controller = new QTEController(e.QTEId, e.Config);

        // 입력 리더 생성
        _inputReader = new QTEInputReader(InputManager.Instance.Inputs, _controller);
    }

    private void OnQTEEnded(QTEEndedEvent e)
    {
        Debug.Log($"[QTEPresenter] OnQTEEnded : {e.QTEId} / {e.Result}");

        // 입력 해제
        _inputReader?.Dispose();
        _inputReader = null;

        // 로직 정리
        _controller = null;

        // UI 숨김
        if (qteRoot != null)
            qteRoot.SetActive(false);

        // QTEEndedEvent는 Controller에서 이미 완성된 형태로 발행됨
    }
}




