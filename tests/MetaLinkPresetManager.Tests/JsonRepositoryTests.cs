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
        Assert.Equal(0.8m, preset.Settings.FovTanMultiplierHorizontal);
        Assert.Equal(AswMode.Off, preset.Settings.AswMode);
    }

    [Fact]
    public async Task LegacyLaunchFields_AreIgnoredWhenLoadingJson()
    {
        var paths = new AppPaths(_folder);
        paths.EnsureCreated();
        await File.WriteAllTextAsync(
            paths.PresetsFile,
            """
            {
              "presets": [
                {
                  "id": "legacy-launch",
                  "name": "Legacy Launch",
                  "launchType": "SteamUrl",
                  "launchTarget": "steam://rungameid/123",
                  "settings": {
                    "videoCodec": "H265"
                  }
                }
              ]
            }
            """);

        var repository = new JsonPresetRepository(paths);
        var preset = Assert.Single((await repository.LoadAsync()).Presets);

        Assert.Equal("legacy-launch", preset.Id);
        Assert.Equal(VideoCodecMode.H265, preset.Settings.VideoCodec);
    }

    [Fact]
    public async Task LegacyAv1Codec_IsPreservedWhenLoadingJson()
    {
        var paths = new AppPaths(_folder);
        paths.EnsureCreated();
        await File.WriteAllTextAsync(
            paths.PresetsFile,
            """
            {
              "presets": [
                {
                  "id": "legacy-av1",
                  "name": "Legacy AV1",
                  "settings": {
                    "videoCodec": "Av1"
                  }
                }
              ]
            }
            """);

        var repository = new JsonPresetRepository(paths);
        var preset = Assert.Single((await repository.LoadAsync()).Presets);

        Assert.Equal(VideoCodecMode.Av1, preset.Settings.VideoCodec);
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

    [Theory]
    [InlineData(LocalDimmingMode.Default)]
    [InlineData(LocalDimmingMode.Disabled)]
    [InlineData(LocalDimmingMode.Enabled)]
    public async Task LocalDimming_RoundTripsThroughJson(LocalDimmingMode mode)
    {
        var repository = new JsonPresetRepository(new AppPaths(_folder));
        var expected = new PresetStore
        {
            Presets =
            [
                new OculusPreset
                {
                    Id = "local-dimming",
                    Name = "Local Dimming",
                    Settings = new OculusSettings
                    {
                        LocalDimming = mode
                    }
                }
            ]
        };

        await repository.SaveAsync(expected);
        var actual = await repository.LoadAsync();

        Assert.Equal(mode, Assert.Single(actual.Presets).Settings.LocalDimming);
    }

    [Fact]
    public async Task ExpandedSettings_RoundTripThroughJson()
    {
        var repository = new JsonPresetRepository(new AppPaths(_folder));
        var settings = new OculusSettings
        {
            PixelsPerDisplayPixelOverride = 1.2m,
            ForceMipmapGenerationOnAllLayers = true,
            OffsetMipmapBiasOnAllLayers = false,
            UseFovStencil = true,
            BypassProximitySensorCheck = false,
            AdaptiveGpuPerformanceScale = true,
            PcAsynchronousSpacewarp = PcAswMode.Force45WithAsw,
            FrameDropIndicator = true,
            DebugHmdType = DebugHmdType.Quest3,
            PoseInjection = false,
            DistortionCurvature = DistortionCurvatureMode.Low,
            VideoCodec = VideoCodecMode.H265,
            SlicedEncoding = true,
            EncodeDynamicBitrate = true,
            DynamicBitrateMax = 300,
            DynamicBitrateOffset = 20,
            VisibleHud = VisibleHudMode.Performance,
            PerformanceHud = PerformanceHudMode.AswStats,
            StereoDebugHud = StereoDebugHudMode.Quad,
            LayerHud = LayerHudMode.ShowAllLayers,
            LostFrameCapture = true
        };
        var store = new PresetStore
        {
            Presets =
            [
                new OculusPreset
                {
                    Id = "expanded",
                    Name = "Expanded",
                    Settings = settings
                }
            ]
        };

        await repository.SaveAsync(store);
        var actual = (await repository.LoadAsync()).Presets.Single().Settings;

        Assert.Equal(1.2m, actual.PixelsPerDisplayPixelOverride);
        Assert.True(actual.ForceMipmapGenerationOnAllLayers);
        Assert.False(actual.OffsetMipmapBiasOnAllLayers);
        Assert.Equal(PcAswMode.Force45WithAsw, actual.PcAsynchronousSpacewarp);
        Assert.Equal(DebugHmdType.Quest3, actual.DebugHmdType);
        Assert.Equal(VideoCodecMode.H265, actual.VideoCodec);
        Assert.Equal(300, actual.DynamicBitrateMax);
        Assert.Equal(PerformanceHudMode.AswStats, actual.PerformanceHud);
        Assert.Equal(LayerHudMode.ShowAllLayers, actual.LayerHud);
        Assert.True(actual.LostFrameCapture);
    }

    [Fact]
    public async Task LegacyHudSubmode_RoundTripsWithoutVisibleHud()
    {
        var repository = new JsonPresetRepository(new AppPaths(_folder));
        var store = new PresetStore
        {
            Presets =
            [
                new OculusPreset
                {
                    Id = "legacy-hud",
                    Name = "Legacy HUD",
                    Settings = new OculusSettings
                    {
                        PerformanceHud = PerformanceHudMode.PerformanceSummary
                    }
                }
            ]
        };

        await repository.SaveAsync(store);
        var actual = (await repository.LoadAsync()).Presets.Single().Settings;

        Assert.Null(actual.VisibleHud);
        Assert.Equal(
            PerformanceHudMode.PerformanceSummary,
            actual.PerformanceHud);
    }

    [Fact]
    public void DefaultPresetMigration_DisablesHudAndClearsSubmodes()
    {
        var store = new PresetStore
        {
            Presets =
            [
                new OculusPreset
                {
                    Id = "default",
                    Name = "Default",
                    Settings = new OculusSettings
                    {
                        VisibleHud = VisibleHudMode.Performance,
                        PerformanceHud = PerformanceHudMode.PerformanceSummary
                    }
                }
            ]
        };

        var changed = PresetStoreMigration.EnsureDefaultHudDisabled(
            store,
            "default");

        Assert.True(changed);
        var settings = store.Presets.Single().Settings;
        Assert.Equal(VisibleHudMode.None, settings.VisibleHud);
        Assert.Null(settings.PerformanceHud);
        Assert.Null(settings.StereoDebugHud);
        Assert.Null(settings.LayerHud);
        Assert.False(PresetStoreMigration.EnsureDefaultHudDisabled(store, "default"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
