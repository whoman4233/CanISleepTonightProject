using UnityEngine;

public class QTEController
{
    private readonly string _qteId;
    private readonly QTEConfig _config;

    private float _currentTime;
    private float _currentValue;
    private bool _holding;
    private float _timeSinceLastInput;
    private bool _ended; // 중복 종료 방지

    public QTEController(string qteId, QTEConfig config)
    {
        _qteId = qteId;
        _config = config;
        _currentTime = config.TimeLimit;
    }

    public void Tick(float delta)
    {
        if (_ended)
            return;

        _currentTime -= delta;
        _timeSinceLastInput += delta;

        // Mash 감소 로직
        if (_config.Type == QTEType.Mash &&
            _timeSinceLastInput >= _config.DecayDelay &&
            _config.DecayPerSecond > 0f)
        {
            _currentValue -= _config.DecayPerSecond * delta;
            _currentValue = Mathf.Max(0f, _currentValue);

            EventBus.Publish(new QTEProgressChangedEvent
            {
                Current = _currentValue,
                Required = _config.RequiredValue
            });
        }

        EventBus.Publish(new QTETimerChangedEvent
        {
            Remaining = _currentTime,
            Limit = _config.TimeLimit
        });

        if (_currentTime <= 0f)
            End(QTEResult.Timeout);
    }



    public void OnPressed()
    {
        if (_ended)
            return;

        _timeSinceLastInput = 0f;

        if (_config.Type == QTEType.Mash)
            AddProgress(_config.PerPressValue);
    }


    public void OnReleased()
    {
        if (_ended)
            return;

        _holding = false;
    }

    private void AddProgress(float value)
    {
        if (_ended)
            return;

        _currentValue += value;

        EventBus.Publish(new QTEProgressChangedEvent
        {
            Current = _currentValue,
            Required = _config.RequiredValue
        });

        if (_currentValue >= _config.RequiredValue)
            End(QTEResult.Success);
    }

    private void End(QTEResult result)
    {
        if (_ended)
            return;

        _ended = true;

        EventBus.Publish(new QTEEndedEvent
        {
            QTEId = _qteId,
            Result = result
        });
    }
}

