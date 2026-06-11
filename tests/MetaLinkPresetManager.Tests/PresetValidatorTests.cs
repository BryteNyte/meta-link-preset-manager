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
            LaunchType = LaunchType.Exe,
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
            "Launch target is required for the selected launch type.",
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
}
