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

    private static OculusPreset CreatePreset()
    {
        return new OculusPreset
        {
            Name = "Test",
            Settings = new OculusSettings()
        };
    }
}
