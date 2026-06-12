using System.Diagnostics;
using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.Core.Services;

public interface IProcessWatcherService : IDisposable
{
    event EventHandler<WatchedProcessEventArgs>? ProcessStarted;
    event EventHandler<WatchedProcessEventArgs>? ProcessExited;
    bool IsRunning { get; }
    void Start();
    void Stop();
    void UpdateInterval(TimeSpan interval);
}

public sealed class ProcessWatcherService(
    Func<IReadOnlyList<OculusPreset>> presetsProvider,
    TimeSpan initialInterval,
    IAppLogger logger) : IProcessWatcherService
{
    private readonly HashSet<string> _runningPresetIds =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();
    private Timer? _timer;
    private TimeSpan _interval = NormalizeInterval(initialInterval);
    private int _polling;

    public event EventHandler<WatchedProcessEventArgs>? ProcessStarted;
    public event EventHandler<WatchedProcessEventArgs>? ProcessExited;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        lock (_sync)
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            _timer = new Timer(Poll, null, TimeSpan.Zero, _interval);
        }

        logger.Info("Process watcher started.");
    }

    public void Stop()
    {
        lock (_sync)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            _timer?.Dispose();
            _timer = null;
            _runningPresetIds.Clear();
        }

        logger.Info("Process watcher stopped.");
    }

    public void UpdateInterval(TimeSpan interval)
    {
        lock (_sync)
        {
            _interval = NormalizeInterval(interval);
            _timer?.Change(TimeSpan.Zero, _interval);
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void Poll(object? state)
    {
        if (Interlocked.Exchange(ref _polling, 1) == 1)
        {
            return;
        }

        try
        {
            var presets = presetsProvider()
                .Where(preset =>
                    preset.Enabled
                    && preset.ApplyAutomaticallyWhenProcessStarts
                    && !string.IsNullOrWhiteSpace(preset.ProcessName))
                .Select(preset => preset.Clone())
                .ToList();

            foreach (var preset in presets)
            {
                var processName = ProcessNameNormalizer.ForProcessLookup(
                    preset.ProcessName!);
                var isRunning = IsProcessRunning(processName);
                bool wasRunning;

                lock (_sync)
                {
                    wasRunning = _runningPresetIds.Contains(preset.Id);
                    if (isRunning)
                    {
                        _runningPresetIds.Add(preset.Id);
                    }
                    else
                    {
                        _runningPresetIds.Remove(preset.Id);
                    }
                }

                if (isRunning && !wasRunning)
                {
                    logger.Info($"Process detected started: {preset.ProcessName}");
                    ProcessStarted?.Invoke(this, new WatchedProcessEventArgs(preset));
                }
                else if (!isRunning && wasRunning)
                {
                    logger.Info($"Process detected exited: {preset.ProcessName}");
                    ProcessExited?.Invoke(this, new WatchedProcessEventArgs(preset));
                }
            }

            RemoveDeletedPresets(presets);
        }
        catch (Exception exception)
        {
            logger.Error("Process watcher poll failed", exception);
        }
        finally
        {
            Interlocked.Exchange(ref _polling, 0);
        }
    }

    private void RemoveDeletedPresets(IReadOnlyCollection<OculusPreset> presets)
    {
        var validIds = presets.Select(preset => preset.Id).ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        lock (_sync)
        {
            _runningPresetIds.RemoveWhere(id => !validIds.Contains(id));
        }
    }

    private static bool IsProcessRunning(string processName)
    {
        Process[] processes = [];
        try
        {
            processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static TimeSpan NormalizeInterval(TimeSpan interval)
    {
        return interval < TimeSpan.FromSeconds(1)
            ? TimeSpan.FromSeconds(1)
            : interval;
    }
}
