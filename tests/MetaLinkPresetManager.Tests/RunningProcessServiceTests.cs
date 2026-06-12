using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

public sealed class RunningProcessServiceTests
{
    [Fact]
    public void FromExecutablePath_StoresOnlyExecutableName()
    {
        var processName = ProcessNameNormalizer.FromExecutablePath(
            @"C:\Games\Example\Game.exe");

        Assert.Equal("Game.exe", processName);
    }

    [Theory]
    [InlineData("Game.exe", "Game")]
    [InlineData("Game", "Game")]
    [InlineData(@"C:\Games\Example\Game.exe", "Game")]
    public void ForProcessLookup_NormalizesSupportedValues(
        string value,
        string expected)
    {
        Assert.Equal(expected, ProcessNameNormalizer.ForProcessLookup(value));
    }

    [Fact]
    public void Provider_ExcludesCurrentProcessAndToleratesProtectedMetadata()
    {
        var source = new FakeProcessSnapshotSource(
        [
            new FakeProcessSnapshot(10, "zeta", "Zeta", @"C:\zeta.exe"),
            new FakeProcessSnapshot(
                20,
                "alpha",
                titleException: new InvalidOperationException(),
                pathException: new UnauthorizedAccessException()),
            new FakeProcessSnapshot(30, "self", "Self", @"C:\self.exe")
        ]);
        var provider = new RunningProcessProvider(source, 30);

        var processes = provider.GetProcesses();

        Assert.Collection(
            processes,
            process =>
            {
                Assert.Equal("alpha.exe", process.ExecutableName);
                Assert.Equal(20, process.ProcessId);
                Assert.Empty(process.WindowTitle);
                Assert.Empty(process.ExecutablePath);
            },
            process =>
            {
                Assert.Equal("zeta.exe", process.ExecutableName);
                Assert.Equal(10, process.ProcessId);
            });
        Assert.All(source.Snapshots, process => Assert.True(process.Disposed));
    }

    [Fact]
    public void ListModel_RefreshReplacesRowsWithoutDuplicates()
    {
        var provider = new SequenceProcessProvider(
        [
            [
                new RunningProcessInfo("First.exe", 1, "", "")
            ],
            [
                new RunningProcessInfo("Second.exe", 2, "", "")
            ]
        ]);
        var model = new RunningProcessListModel(provider);

        model.Refresh();
        model.Refresh();

        var process = Assert.Single(model.Processes);
        Assert.Equal("Second.exe", process.ExecutableName);
    }

    private sealed class FakeProcessSnapshotSource(
        IReadOnlyList<FakeProcessSnapshot> snapshots) : IProcessSnapshotSource
    {
        public IReadOnlyList<FakeProcessSnapshot> Snapshots { get; } = snapshots;

        public IReadOnlyList<IProcessSnapshot> GetProcesses()
        {
            return Snapshots.Cast<IProcessSnapshot>().ToList();
        }
    }

    private sealed class FakeProcessSnapshot(
        int id,
        string processName,
        string title = "",
        string path = "",
        Exception? titleException = null,
        Exception? pathException = null) : IProcessSnapshot
    {
        public int Id => id;
        public string ProcessName => processName;
        public string MainWindowTitle =>
            titleException is null ? title : throw titleException;
        public string ExecutablePath =>
            pathException is null ? path : throw pathException;
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    private sealed class SequenceProcessProvider(
        IReadOnlyList<IReadOnlyList<RunningProcessInfo>> results)
        : IRunningProcessProvider
    {
        private int _index;

        public IReadOnlyList<RunningProcessInfo> GetProcesses()
        {
            var index = Math.Min(_index, results.Count - 1);
            _index++;
            return results[index];
        }
    }
}
