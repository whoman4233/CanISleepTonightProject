using UnityEngine.InputSystem;

public class QTEInputReader
{
    private readonly PlayerInputs.QTEActions _inputs;
    private readonly QTEController _controller;

    private bool _disposed;

    public QTEInputReader(PlayerInputs inputs, QTEController controller)
    {
        _inputs = inputs.QTE;
        _controller = controller;

        _inputs.Confirm.performed += OnPressed;
        _inputs.Confirm.canceled += OnReleased;
    }

    private void OnPressed(InputAction.CallbackContext ctx)
    {
        if (_disposed)
            return;

        _controller.OnPressed();

        EventBus.Publish(new QTEInputFeedbackEvent
        {
            State = QTEInputState.Pressed
        });
    }

    private void OnReleased(InputAction.CallbackContext ctx)
    {
        if (_disposed)
            return;

        _controller.OnReleased();

        EventBus.Publish(new QTEInputFeedbackEvent
        {
            State = QTEInputState.Released
        });
    }

    /// <summary>
    /// QTE 종료 시 반드시 호출해야 함
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _inputs.Confirm.performed -= OnPressed;
        _inputs.Confirm.canceled -= OnReleased;
    }
}
