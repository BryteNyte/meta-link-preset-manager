using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

internal sealed class TestLogger : IAppLogger
{
    public event EventHandler<string>? LogWritten;
    public List<string> Messages { get; } = [];

    public void Info(string message)
    {
        Messages.Add(message);
        LogWritten?.Invoke(this, message);
    }

    public void Error(string message, Exception? exception = null)
    {
        Messages.Add(
            exception is null ? message : $"{message}: {exception.Message}");
        LogWritten?.Invoke(this, message);
    }
}
