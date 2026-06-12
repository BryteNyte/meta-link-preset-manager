using System.Diagnostics;

namespace MetaLinkPresetManager.Core.Services;

public sealed record RunningProcessInfo(
    string ExecutableName,
    int ProcessId,
    string WindowTitle,
    string ExecutablePath);

public interface IProcessSnapshot : IDisposable
{
    int Id { get; }
    string ProcessName { get; }
    string MainWindowTitle { get; }
    string ExecutablePath { get; }
}

public interface IProcessSnapshotSource
{
    IReadOnlyList<IProcessSnapshot> GetProcesses();
}

public sealed class SystemProcessSnapshotSource : IProcessSnapshotSource
{
    public IReadOnlyList<IProcessSnapshot> GetProcesses()
    {
        return Process.GetProcesses()
            .Select(process => (IProcessSnapshot)new SystemProcessSnapshot(process))
            .ToList();
    }

    private sealed class SystemProcessSnapshot(Process process)
        : IProcessSnapshot
    {
        public int Id => process.Id;
        public string ProcessName => process.ProcessName;
        public string MainWindowTitle => process.MainWindowTitle;
        public string ExecutablePath => process.MainModule?.FileName ?? string.Empty;

        public void Dispose()
        {
            process.Dispose();
        }
    }
}

public interface IRunningProcessProvider
{
    IReadOnlyList<RunningProcessInfo> GetProcesses();
}

public sealed class RunningProcessProvider(
    IProcessSnapshotSource source,
    int excludedProcessId) : IRunningProcessProvider
{
    public IReadOnlyList<RunningProcessInfo> GetProcesses()
    {
        var results = new List<RunningProcessInfo>();
        foreach (var process in source.GetProcesses())
        {
            using (process)
            {
                var processId = ReadOrDefault(() => process.Id, -1);
                if (processId == excludedProcessId)
                {
                    continue;
                }

                var processName = ReadOrDefault(
                    () => process.ProcessName,
                    string.Empty);
                if (string.IsNullOrWhiteSpace(processName))
                {
                    continue;
                }

                results.Add(new RunningProcessInfo(
                    ProcessNameNormalizer.FromProcessName(processName),
                    processId,
                    ReadOrDefault(() => process.MainWindowTitle, string.Empty),
                    ReadOrDefault(() => process.ExecutablePath, string.Empty)));
            }
        }

        return results
            .OrderBy(process => process.ExecutableName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(process => process.ProcessId)
            .ToList();
    }

    private static T ReadOrDefault<T>(Func<T> read, T fallback)
    {
        try
        {
            return read();
        }
        catch
        {
            return fallback;
        }
    }
}

public sealed class RunningProcessListModel(IRunningProcessProvider provider)
{
    private IReadOnlyList<RunningProcessInfo> _processes = [];

    public IReadOnlyList<RunningProcessInfo> Processes => _processes;

    public void Refresh()
    {
        _processes = provider.GetProcesses().ToList();
    }
}

public static class ProcessNameNormalizer
{
    public static string FromExecutablePath(string path)
    {
        return Path.GetFileName(path.Trim());
    }

    public static string FromProcessName(string processName)
    {
        var fileName = Path.GetFileName(processName.Trim());
        return Path.HasExtension(fileName) ? fileName : fileName + ".exe";
    }

    public static string ForProcessLookup(string processName)
    {
        return Path.GetFileNameWithoutExtension(processName.Trim());
    }
}
