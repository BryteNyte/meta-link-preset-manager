using MetaLinkPresetManager.Core.Models;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.Tests;

public sealed class PresetValidatorTests
{
    [Fact]
    public void ValidateForSave_RejectsIncompleteAutomaticPreset()
    {
        var preset = new OculusPreset
        {
            Name = "",
            ApplyAutomaticallyWhenProcessStarts = true,
            Settings = new OculusSettings
            {
                FovTanMultiplierHorizontal = 0.8m
            }
        };

        var errors = PresetValidator.ValidateForSave(preset, []);

        Assert.Contains("Preset name is required.", errors);
        Assert.Contains(
            "Process name is required when automatic apply is enabled.",
            errors);
        Assert.Contains(
            "FOV horizontal and vertical must both be included or both omitted.",
            errors);
    }

    [Fact]
    public void ValidateForSave_AcceptsCompletePreset()
    {
        var preset = new OculusPreset
        {
            Name = "Complete",
            ProcessName = "game.exe",
            ApplyAutomaticallyWhenProcessStarts = true,
            Settings = new OculusSettings
            {
                FovTanMultiplierHorizontal = 0.8m,
                FovTanMultiplierVertical = 0.7m,
                AswMode = AswMode.Off
            }
        };

        var errors = PresetValidator.ValidateForSave(preset, []);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateForApply_AcceptsCodecOnlyPresetWithoutCli()
    {
        var preset = new OculusPreset
        {
            Name = "Stored only",
            Settings = new OculusSettings
            {
                VideoCodec = VideoCodecMode.H265
            }
        };

        var errors = PresetValidator.ValidateForApply(
            preset,
            typeof(PresetValidator).Assembly.Location);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(VisibleHudMode.Performance)]
    [InlineData(VisibleHudMode.StereoDebug)]
    [InlineData(VisibleHudMode.Layer)]
    public void ValidateForSave_RejectsHudWithoutRequiredMode(
        VisibleHudMode visibleHud)
    {
        var preset = new OculusPreset
        {
            Name = "Invalid HUD",
            ApplyAutomaticallyWhenProcessStarts = false,
            Settings = new OculusSettings
            {
                VisibleHud = visibleHud
            }
        };

        var errors = PresetValidator.ValidateForSave(preset, []);

        Assert.Single(errors);
        Assert.Contains("mode is required", errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForApply_AcceptsPerformanceHud()
    {
        var preset = new OculusPreset
        {
            Name = "Performance HUD",
            Settings = new OculusSettings
            {
                VisibleHud = VisibleHudMode.Performance,
                PerformanceHud = PerformanceHudMode.PerformanceSummary
            }
        };

        var errors = PresetValidator.ValidateForApply(
            preset,
            typeof(PresetValidator).Assembly.Location);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateForApply_RejectsUnsupportedAv1Codec()
    {
        var preset = new OculusPreset
        {
            Name = "AV1",
            ApplyAutomaticallyWhenProcessStarts = false,
            Settings = new OculusSettings
            {
                VideoCodec = VideoCodecMode.Av1
            }
        };

        var errors = PresetValidator.ValidateForApply(preset, "missing-cli.exe");

        Assert.Contains(
            "AV1 video codec is preserved for JSON compatibility but is not supported by the installed Meta Debug Tool.",
            errors);
    }
}
