namespace MetaLinkPresetManager.Core.Services;

public interface IAppLogger
{
    event EventHandler<string>? LogWritten;
    void Info(string message);
    void Error(string message, Exception? exception = null);
}

public sealed class FileLogger(string logsFolder) : IAppLogger
{
    private readonly object _sync = new();

    public event EventHandler<string>? LogWritten;

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message, Exception? exception = null)
    {
        var detail = exception is null ? message : $"{message}: {exception.Message}";
        Write("ERROR", detail);
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        lock (_sync)
        {
            Directory.CreateDirectory(logsFolder);
            var path = Path.Combine(logsFolder, $"app-{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(path, line + Environment.NewLine);
        }

        LogWritten?.Invoke(this, line);
    }
}
