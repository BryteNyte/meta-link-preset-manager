using MetaLinkPresetManager.Core.Infrastructure;
using MetaLinkPresetManager.Core.Models;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

public sealed class OculusDebugToolServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(
        Path.GetTempPath(),
        "MetaLinkPresetManagerServiceTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ApplyPreset_CodecOnlyDoesNotRequireOrRunCli()
    {
        var registry = new FakeCodecRegistryService();
        var runner = new FakeCliRunner();
        var service = CreateService("missing-cli.exe", registry, runner);
        var preset = CreatePreset(VideoCodecMode.H265);

        var result = await service.ApplyPresetAsync(preset);

        Assert.True(result.Succeeded);
        Assert.Null(result.CommandFilePath);
        Assert.Equal(VideoCodecMode.H265, registry.AppliedCodec);
        Assert.Equal(0, registry.RestoreCount);
        Assert.Equal(0, runner.RunCount);
    }

    [Fact]
    public async Task ApplyPreset_MixedSettingsAppliesRegistryAndCli()
    {
        var registry = new FakeCodecRegistryService();
        var runner = new FakeCliRunner();
        var service = CreateService(
            typeof(OculusDebugToolServiceTests).Assembly.Location,
            registry,
            runner);
        var preset = CreatePreset(VideoCodecMode.H264);
        preset.Settings.AswMode = AswMode.Off;

        var result = await service.ApplyPresetAsync(preset);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.CommandFilePath);
        Assert.Equal(VideoCodecMode.H264, registry.AppliedCodec);
        Assert.Equal(0, registry.RestoreCount);
        Assert.Equal(1, runner.RunCount);
    }

    [Fact]
    public async Task ApplyPreset_UncheckedCodecLeavesRegistryUnchanged()
    {
        var registry = new FakeCodecRegistryService();
        var runner = new FakeCliRunner();
        var service = CreateService(
            typeof(OculusDebugToolServiceTests).Assembly.Location,
            registry,
            runner);
        var preset = new OculusPreset
        {
            Name = "CLI only",
            Settings = new OculusSettings
            {
                AswMode = AswMode.Off
            }
        };

        var result = await service.ApplyPresetAsync(preset);

        Assert.True(result.Succeeded);
        Assert.Equal(0, registry.CaptureCount);
        Assert.Null(registry.AppliedCodec);
        Assert.Equal(0, registry.RestoreCount);
    }

    [Fact]
    public async Task ApplyPreset_CliFailureRestoresCodecSnapshot()
    {
        var registry = new FakeCodecRegistryService();
        var runner = new FakeCliRunner
        {
            Result = new CliExecutionResult(false, "CLI failed.")
        };
        var service = CreateService(
            typeof(OculusDebugToolServiceTests).Assembly.Location,
            registry,
            runner);
        var preset = CreatePreset(VideoCodecMode.H265);
        preset.Settings.AswMode = AswMode.Auto;

        var result = await service.ApplyPresetAsync(preset);

        Assert.False(result.Succeeded);
        Assert.Equal("CLI failed.", result.ErrorMessage);
        Assert.Equal(1, registry.RestoreCount);
        Assert.Same(registry.Snapshot, registry.RestoredSnapshot);
    }

    [Fact]
    public async Task ApplyPreset_CancellationRestoresCodecSnapshot()
    {
        var registry = new FakeCodecRegistryService();
        var runner = new FakeCliRunner { Cancel = true };
        var service = CreateService(
            typeof(OculusDebugToolServiceTests).Assembly.Location,
            registry,
            runner);
        var preset = CreatePreset(VideoCodecMode.Default);
        preset.Settings.AswMode = AswMode.Auto;

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.ApplyPresetAsync(preset));

        Assert.Equal(1, registry.RestoreCount);
        Assert.Same(registry.Snapshot, registry.RestoredSnapshot);
    }

    private OculusDebugToolService CreateService(
        string cliPath,
        IVideoCodecRegistryService registry,
        IOculusCliRunner runner)
    {
        return new OculusDebugToolService(
            new AppSettings
            {
                OculusDebugToolCliPath = cliPath,
                RequireElevationForCli = false
            },
            new AppPaths(_folder),
            new OculusCommandFileBuilder(),
            registry,
            runner,
            new TestLogger());
    }

    private static OculusPreset CreatePreset(VideoCodecMode codec)
    {
        return new OculusPreset
        {
            Name = "Codec test",
            Settings = new OculusSettings
            {
                VideoCodec = codec
            }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private sealed class FakeCodecRegistryService
        : IVideoCodecRegistryService
    {
        public RegistryValueSnapshot Snapshot { get; } =
            new(true, 0, 4);
        public VideoCodecMode? AppliedCodec { get; private set; }
        public int CaptureCount { get; private set; }
        public int RestoreCount { get; private set; }
        public RegistryValueSnapshot? RestoredSnapshot { get; private set; }

        public RegistryValueSnapshot Capture()
        {
            CaptureCount++;
            return Snapshot;
        }

        public void Apply(VideoCodecMode codec)
        {
            AppliedCodec = codec;
        }

        public void Restore(RegistryValueSnapshot snapshot)
        {
            RestoreCount++;
            RestoredSnapshot = snapshot;
        }
    }

    private sealed class FakeCliRunner : IOculusCliRunner
    {
        public CliExecutionResult Result { get; set; } =
            new(true, ProcessOutput: "ok");
        public bool Cancel { get; set; }
        public int RunCount { get; private set; }

        public Task<CliExecutionResult> RunAsync(
            string cliPath,
            string commandFile,
            bool requireElevation,
            CancellationToken cancellationToken = default)
        {
            RunCount++;
            if (Cancel)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(Result);
        }
    }
}
