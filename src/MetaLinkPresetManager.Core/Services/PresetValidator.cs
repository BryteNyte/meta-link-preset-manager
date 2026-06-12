using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.Core.Services;

public static class PresetValidator
{
    public static IReadOnlyList<string> ValidateForSave(
        OculusPreset preset,
        IEnumerable<OculusPreset> existingPresets)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            errors.Add("Preset name is required.");
        }

        if (string.IsNullOrWhiteSpace(preset.Id))
        {
            errors.Add("Preset ID is required.");
        }
        else if (existingPresets.Any(x =>
                     !ReferenceEquals(x, preset)
                     && string.Equals(x.Id, preset.Id, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("Preset ID must be unique.");
        }

        if (preset.ApplyAutomaticallyWhenProcessStarts
            && string.IsNullOrWhiteSpace(preset.ProcessName))
        {
            errors.Add("Process name is required when automatic apply is enabled.");
        }

        var hasHorizontal = preset.Settings.FovTanMultiplierHorizontal.HasValue;
        var hasVertical = preset.Settings.FovTanMultiplierVertical.HasValue;
        if (hasHorizontal != hasVertical)
        {
            errors.Add("FOV horizontal and vertical must both be included or both omitted.");
        }

        if (preset.Settings.EncodeBitrateMbps < 0)
        {
            errors.Add("Encode bitrate must be zero or greater.");
        }

        if (preset.Settings.EncodeResolutionWidth < 0)
        {
            errors.Add("Encode resolution width must be zero or greater.");
        }

        if (preset.Settings.PixelsPerDisplayPixelOverride < 0)
        {
            errors.Add("Pixels per display pixel override must be zero or greater.");
        }

        if (preset.Settings.DynamicBitrateMax < 0)
        {
            errors.Add("Dynamic bitrate max must be zero or greater.");
        }

        if (preset.Settings.DynamicBitrateOffset < 0)
        {
            errors.Add("Dynamic bitrate offset must be zero or greater.");
        }

        ValidateHudSettings(preset.Settings, errors);
        return errors;
    }

    public static IReadOnlyList<string> ValidateForApply(OculusPreset preset, string cliPath)
    {
        var errors = new List<string>();

        if (preset.Settings.HasCliApplicableValue() && !File.Exists(cliPath))
        {
            errors.Add($"OculusDebugToolCLI.exe was not found at: {cliPath}");
        }

        if (!preset.Settings.HasAnyValue())
        {
            errors.Add("The preset does not include any Oculus settings.");
        }
        else if (!preset.Settings.HasCliApplicableValue()
                 && !preset.Settings.HasRegistryApplicableValue())
        {
            errors.Add(
                "The preset only contains settings that cannot be applied by this installed Meta runtime.");
        }

        if (preset.Settings.VideoCodec == VideoCodecMode.Av1)
        {
            errors.Add(
                "AV1 video codec is preserved for JSON compatibility but is not supported by the installed Meta Debug Tool.");
        }

        var hasHorizontal = preset.Settings.FovTanMultiplierHorizontal.HasValue;
        var hasVertical = preset.Settings.FovTanMultiplierVertical.HasValue;
        if (hasHorizontal != hasVertical)
        {
            errors.Add("FOV horizontal and vertical must both be included or both omitted.");
        }

        ValidateHudSettings(preset.Settings, errors);
        return errors;
    }

    private static void ValidateHudSettings(
        OculusSettings settings,
        ICollection<string> errors)
    {
        if (!settings.VisibleHud.HasValue
            || settings.VisibleHud == VisibleHudMode.None)
        {
            return;
        }

        if (settings.VisibleHud == VisibleHudMode.Performance
            && settings.PerformanceHud is null or PerformanceHudMode.None)
        {
            errors.Add(
                "Performance HUD mode is required when Visible HUD is Performance.");
        }

        if (settings.VisibleHud == VisibleHudMode.StereoDebug
            && settings.StereoDebugHud is null or StereoDebugHudMode.None)
        {
            errors.Add(
                "Stereo Debug HUD mode is required when Visible HUD is Stereo Debug.");
        }

        if (settings.VisibleHud == VisibleHudMode.Layer
            && settings.LayerHud is null or LayerHudMode.None)
        {
            errors.Add(
                "Layer HUD mode is required when Visible HUD is Layer.");
        }
    }
}
