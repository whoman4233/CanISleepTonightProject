public class QTEController
{
    private readonly QTEConfig _config;
    private float _currentTime;
    private float _currentValue;
    private bool _holding;

    public QTEController(QTEConfig config)
    {
        _config = config;
        _currentTime = config.TimeLimit;
    }

    public void Tick(float delta)
    {
        _currentTime -= delta;

        if (_config.Type == QTEType.Hold && _holding)
        {
            AddProgress(_config.HoldPerSecond * delta);
        }

        EventBus.Publish(new QTETimerChangedEvent
        {
            Remaining = _currentTime,
            Limit = _config.TimeLimit
        });

        if (_currentTime <= 0)
            End(QTEResult.Timeout);
    }

    public void OnPressed()
    {
        if (_config.Type == QTEType.Mash)
            AddProgress(_config.PerPressValue);

        _holding = true;
    }

    public void OnReleased()
    {
        _holding = false;
    }

    private void AddProgress(float value)
    {
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
        EventBus.Publish(new QTEEndedEvent
        {
            Result = result
        });
    }
}
