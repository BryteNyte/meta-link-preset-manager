using MetaLinkPresetManager.Core.Infrastructure;
using MetaLinkPresetManager.Core.Models;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

public sealed class JsonRepositoryTests : IDisposable
{
    private readonly string _folder = Path.Combine(
        Path.GetTempPath(),
        "MetaLinkPresetManagerTests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Presets_RoundTripThroughJson()
    {
        var repository = new JsonPresetRepository(new AppPaths(_folder));
        var expected = new PresetStore
        {
            Presets =
            [
                new OculusPreset
                {
                    Id = "test-id",
                    Name = "Round Trip",
                    ProcessName = "game.exe",
                    LaunchType = LaunchType.SteamUrl,
                    LaunchTarget = "steam://rungameid/123",
                    Settings = new OculusSettings
                    {
                        FovTanMultiplierHorizontal = 0.8m,
                        FovTanMultiplierVertical = 0.7m,
                        AswMode = AswMode.Off
                    }
                }
            ]
        };

        await repository.SaveAsync(expected);
        var actual = await repository.LoadAsync();

        var preset = Assert.Single(actual.Presets);
        Assert.Equal("test-id", preset.Id);
        Assert.Equal(LaunchType.SteamUrl, preset.LaunchType);
        Assert.Equal(0.8m, preset.Settings.FovTanMultiplierHorizontal);
        Assert.Equal(AswMode.Off, preset.Settings.AswMode);
    }

    [Fact]
    public async Task Settings_RoundTripThroughJson()
    {
        var repository = new JsonAppSettingsRepository(new AppPaths(_folder));
        var expected = new AppSettings
        {
            OculusDebugToolCliPath = @"C:\Tools\OculusDebugToolCLI.exe",
            RequireElevationForCli = false,
            ProcessPollingIntervalSeconds = 5,
            RestoreDefaultPresetOnAppExit = true,
            DefaultPresetId = "default"
        };

        await repository.SaveAsync(expected);
        var actual = await repository.LoadAsync();

        Assert.Equal(expected.OculusDebugToolCliPath, actual.OculusDebugToolCliPath);
        Assert.False(actual.RequireElevationForCli);
        Assert.Equal(5, actual.ProcessPollingIntervalSeconds);
        Assert.True(actual.RestoreDefaultPresetOnAppExit);
        Assert.Equal("default", actual.DefaultPresetId);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
