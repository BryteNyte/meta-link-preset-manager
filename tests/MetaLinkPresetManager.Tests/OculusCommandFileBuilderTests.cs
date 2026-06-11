using MetaLinkPresetManager.Core.Models;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

public sealed class OculusCommandFileBuilderTests
{
    private readonly OculusCommandFileBuilder _builder = new();

    [Fact]
    public void BuildCommands_WithFovAndAswOff_GeneratesExpectedCommands()
    {
        var preset = CreatePreset();
        preset.Settings.FovTanMultiplierHorizontal = 0.8m;
        preset.Settings.FovTanMultiplierVertical = 0.7m;
        preset.Settings.AswMode = AswMode.Off;

        var commands = _builder.BuildCommands(preset);

        Assert.Contains(
            "service set-client-fov-tan-angle-multiplier 0.80 0.70",
            commands);
        Assert.Contains("server:asw.Off", commands);
        Assert.Equal("exit", commands[^1]);
    }

    [Fact]
    public void BuildCommands_WithOnlyOneFovValue_OmitsFovCommand()
    {
        var preset = CreatePreset();
        preset.Settings.FovTanMultiplierHorizontal = 0.8m;

        var commands = _builder.BuildCommands(preset);

        Assert.DoesNotContain(
            commands,
            command => command.StartsWith(
                "service set-client-fov-tan-angle-multiplier",
                StringComparison.Ordinal));
        Assert.Equal(["exit"], commands);
    }

    [Theory]
    [InlineData(AswMode.Auto, "server:asw.Auto")]
    [InlineData(AswMode.Off, "server:asw.Off")]
    [InlineData(AswMode.Force45, "server:asw.Clock45")]
    [InlineData(AswMode.Force45WithAsw, "server:asw.Sim45")]
    [InlineData(AswMode.Disabled, "server:asw.Disabled")]
    public void BuildCommands_MapsAswModes(AswMode mode, string expected)
    {
        var preset = CreatePreset();
        preset.Settings.AswMode = mode;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(expected, commands[0]);
    }

    [Theory]
    [InlineData(LinkSharpeningMode.Default, "service set-link-sharpening default")]
    [InlineData(LinkSharpeningMode.Disabled, "service set-link-sharpening disabled")]
    [InlineData(LinkSharpeningMode.Normal, "service set-link-sharpening normal")]
    [InlineData(LinkSharpeningMode.Quality, "service set-link-sharpening quality")]
    public void BuildCommands_MapsLinkSharpening(
        LinkSharpeningMode mode,
        string expected)
    {
        var preset = CreatePreset();
        preset.Settings.LinkSharpening = mode;

        Assert.Equal(expected, _builder.BuildCommands(preset)[0]);
    }

    [Theory]
    [InlineData(LocalDimmingMode.Default, "service set-local-dimming default")]
    [InlineData(LocalDimmingMode.Disabled, "service set-local-dimming disabled")]
    [InlineData(LocalDimmingMode.Enabled, "service set-local-dimming enabled")]
    public void BuildCommands_MapsLocalDimming(
        LocalDimmingMode mode,
        string expected)
    {
        var preset = CreatePreset();
        preset.Settings.LocalDimming = mode;

        Assert.Equal(expected, _builder.BuildCommands(preset)[0]);
    }

    [Fact]
    public void BuildCommands_WithAllNullableSettingsMissing_OnlyExits()
    {
        var commands = _builder.BuildCommands(CreatePreset());

        Assert.Equal(["exit"], commands);
    }

    [Fact]
    public void BuildCommands_WithConfirmedServiceSettings_GeneratesExpectedCommands()
    {
        var preset = CreatePreset();
        preset.Settings.PixelsPerDisplayPixelOverride = 1.25m;
        preset.Settings.ForceMipmapGenerationOnAllLayers = true;
        preset.Settings.OffsetMipmapBiasOnAllLayers = false;
        preset.Settings.UseFovStencil = true;
        preset.Settings.AdaptiveGpuPerformanceScale = false;
        preset.Settings.FrameDropIndicator = true;
        preset.Settings.PoseInjection = false;

        var commands = _builder.BuildCommands(preset);

        Assert.Contains(
            "service set-pixels-per-display-pixel-override 1.25",
            commands);
        Assert.Contains(
            "service set-force-mip-gen-on-all-layers true",
            commands);
        Assert.Contains(
            "service set-offset-mip-bias-on-all-layers false",
            commands);
        Assert.Contains("service set-use-fov-stencil true", commands);
        Assert.Contains(
            "service enable-adaptive-gpu-perf-scale false",
            commands);
        Assert.Contains("server:FrameDropHUDEnabled true", commands);
        Assert.Contains("service set-pose-injection 0", commands);
    }

    [Fact]
    public void BuildCommands_WithStoredOnlySettings_DoesNotGuessCommands()
    {
        var preset = CreatePreset();
        preset.Settings.VideoCodec = VideoCodecMode.H265;
        preset.Settings.PcAsynchronousSpacewarp = PcAswMode.Auto;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(["exit"], commands);
    }

    [Theory]
    [InlineData(PerformanceHudMode.PerformanceSummary, 1)]
    [InlineData(PerformanceHudMode.LatencyTiming, 2)]
    [InlineData(PerformanceHudMode.AppRenderTiming, 3)]
    [InlineData(PerformanceHudMode.CompositorRenderTiming, 4)]
    [InlineData(PerformanceHudMode.VersionInfo, 5)]
    [InlineData(PerformanceHudMode.AswStats, 6)]
    public void BuildCommands_MapsPerformanceHudModes(
        PerformanceHudMode mode,
        int expectedMode)
    {
        var preset = CreatePreset();
        preset.Settings.VisibleHud = VisibleHudMode.Performance;
        preset.Settings.PerformanceHud = mode;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(
            [
                "perfhud reset",
                "stereohud set-mode 0",
                "layerhud reset",
                $"perfhud set-mode {expectedMode}",
                "exit"
            ],
            commands);
    }

    [Theory]
    [InlineData(StereoDebugHudMode.Quad, 1)]
    [InlineData(StereoDebugHudMode.QuadWithCrosshair, 2)]
    [InlineData(StereoDebugHudMode.CrosshairAtInfinity, 3)]
    public void BuildCommands_MapsStereoDebugHudModes(
        StereoDebugHudMode mode,
        int expectedMode)
    {
        var preset = CreatePreset();
        preset.Settings.VisibleHud = VisibleHudMode.StereoDebug;
        preset.Settings.StereoDebugHud = mode;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(
            [
                "perfhud reset",
                "stereohud set-mode 0",
                "layerhud reset",
                $"stereohud set-mode {expectedMode}",
                "exit"
            ],
            commands);
    }

    [Theory]
    [InlineData(LayerHudMode.LayerInfo, false)]
    [InlineData(LayerHudMode.ShowAllLayers, true)]
    public void BuildCommands_MapsLayerHudModes(
        LayerHudMode mode,
        bool showAllLayers)
    {
        var preset = CreatePreset();
        preset.Settings.VisibleHud = VisibleHudMode.Layer;
        preset.Settings.LayerHud = mode;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(
            [
                "perfhud reset",
                "stereohud set-mode 0",
                "layerhud reset",
                "layerhud set-mode 1",
                $"layerhud show-all-layers {showAllLayers.ToString().ToLowerInvariant()}",
                "exit"
            ],
            commands);
    }

    [Fact]
    public void BuildCommands_WithVisibleHudNone_DisablesEveryHud()
    {
        var preset = CreatePreset();
        preset.Settings.VisibleHud = VisibleHudMode.None;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(
            [
                "perfhud reset",
                "stereohud set-mode 0",
                "layerhud reset",
                "exit"
            ],
            commands);
    }

    [Fact]
    public void BuildCommands_WithHudUnchecked_LeavesHudUnchanged()
    {
        var preset = CreatePreset();
        preset.Settings.PerformanceHud = PerformanceHudMode.PerformanceSummary;

        var commands = _builder.BuildCommands(preset);

        Assert.Equal(["exit"], commands);
    }

    private static OculusPreset CreatePreset()
    {
        return new OculusPreset
        {
            Name = "Test",
            Settings = new OculusSettings()
        };
    }
}
