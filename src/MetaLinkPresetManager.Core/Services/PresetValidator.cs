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

        if (preset.LaunchType != LaunchType.None
            && string.IsNullOrWhiteSpace(preset.LaunchTarget))
        {
            errors.Add("Launch target is required for the selected launch type.");
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

        return errors;
    }

    public static IReadOnlyList<string> ValidateForApply(OculusPreset preset, string cliPath)
    {
        var errors = new List<string>();

        if (!File.Exists(cliPath))
        {
            errors.Add($"OculusDebugToolCLI.exe was not found at: {cliPath}");
        }

        if (!preset.Settings.HasAnyValue())
        {
            errors.Add("The preset does not include any Oculus settings.");
        }

        var hasHorizontal = preset.Settings.FovTanMultiplierHorizontal.HasValue;
        var hasVertical = preset.Settings.FovTanMultiplierVertical.HasValue;
        if (hasHorizontal != hasVertical)
        {
            errors.Add("FOV horizontal and vertical must both be included or both omitted.");
        }

        return errors;
    }
}
