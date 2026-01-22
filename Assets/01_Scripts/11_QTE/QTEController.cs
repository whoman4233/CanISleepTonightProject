using UnityEngine;

public class QTEController
{
    // ID + Config 제거
    private readonly QTEActionSO _action;

    private float _currentTime;
    private float _currentValue;
    private float _timeSinceLastInput;
    private bool _ended;

    public QTEController(QTEActionSO action)
    {
        _action = action;

        //  초기값 설정
        _currentTime = action.timeLimit;
        _currentValue = 0f;
        _timeSinceLastInput = 0f;
        _ended = false;
    }

    public void Tick(float delta)
    {
        if (_ended)
            return;

        _currentTime -= delta;
        _timeSinceLastInput += delta;

        // Mash 감소 로직
        if (_action.type == QTEType.Mash &&
            _timeSinceLastInput >= _action.decayDelay &&
            _action.decayPerSecond > 0f)
        {
            _currentValue -= _action.decayPerSecond * delta;
            _currentValue = Mathf.Max(0f, _currentValue);

            EventBus.Publish(new QTEProgressChangedEvent
            {
                Current = _currentValue,
                Required = _action.requiredValue
            });
        }

        EventBus.Publish(new QTETimerChangedEvent
        {
            Remaining = _currentTime,
            Limit = _action.timeLimit
        });

        if (_currentTime <= 0f)
            End(QTEResult.Timeout);
    }

    public void OnPressed()
    {
        if (_ended)
            return;

        _timeSinceLastInput = 0f;

        if (_action.type == QTEType.Mash)
            AddProgress(_action.perPressValue);
    }

    public void OnReleased()
    {
        if (_ended)
            return;
    }

    private void AddProgress(float value)
    {
        if (_ended)
            return;

        _currentValue += value;

        EventBus.Publish(new QTEProgressChangedEvent
        {
            Current = _currentValue,
            Required = _action.requiredValue
        });

        if (_currentValue >= _action.requiredValue)
            End(QTEResult.Success);
    }

    private void End(QTEResult result)
    {
        if (_ended)
            return;

        _ended = true;

        EventBus.Publish(new QTEEndedEvent
        {
            Action = _action,
            Result = result
        });
    }
}

