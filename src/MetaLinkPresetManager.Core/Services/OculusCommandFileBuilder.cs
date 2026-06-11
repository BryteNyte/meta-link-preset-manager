using System.Globalization;
using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.Core.Services;

public interface ICommandFileBuilder
{
    IReadOnlyList<string> BuildCommands(OculusPreset preset);
}

public sealed class OculusCommandFileBuilder : ICommandFileBuilder
{
    public IReadOnlyList<string> BuildCommands(OculusPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);

        var commands = new List<string>();
        var settings = preset.Settings;

        if (settings.FovTanMultiplierHorizontal.HasValue
            && settings.FovTanMultiplierVertical.HasValue)
        {
            commands.Add(string.Format(
                CultureInfo.InvariantCulture,
                "service set-client-fov-tan-angle-multiplier {0:0.00} {1:0.00}",
                settings.FovTanMultiplierHorizontal.Value,
                settings.FovTanMultiplierVertical.Value));
        }

        if (settings.AswMode.HasValue)
        {
            commands.Add(MapAswCommand(settings.AswMode.Value));
        }

        if (settings.EncodeBitrateMbps.HasValue)
        {
            commands.Add($"service set-encode-bitrate-mbps {settings.EncodeBitrateMbps.Value}");
        }

        if (settings.EncodeResolutionWidth.HasValue)
        {
            commands.Add($"service set-encode-resolution-width {settings.EncodeResolutionWidth.Value}");
        }

        if (settings.LinkSharpening.HasValue)
        {
            commands.Add(MapLinkSharpeningCommand(settings.LinkSharpening.Value));
        }

        if (settings.LocalDimming.HasValue)
        {
            commands.Add(MapLocalDimmingCommand(settings.LocalDimming.Value));
        }

        commands.Add("exit");
        return commands;
    }

    private static string MapAswCommand(AswMode mode)
    {
        return mode switch
        {
            AswMode.Auto => "server:asw.Auto",
            AswMode.Off => "server:asw.Off",
            AswMode.Force45 => "server:asw.Clock45",
            AswMode.Force45WithAsw => "server:asw.Sim45",
            AswMode.Disabled => "server:asw.Disabled",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static string MapLinkSharpeningCommand(LinkSharpeningMode mode)
    {
        return mode switch
        {
            LinkSharpeningMode.Default => "service set-link-sharpening default",
            LinkSharpeningMode.Disabled => "service set-link-sharpening disabled",
            LinkSharpeningMode.Normal => "service set-link-sharpening normal",
            LinkSharpeningMode.Quality => "service set-link-sharpening quality",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static string MapLocalDimmingCommand(LocalDimmingMode mode)
    {
        return mode switch
        {
            LocalDimmingMode.Default => "service set-local-dimming default",
            LocalDimmingMode.Disabled => "service set-local-dimming disabled",
            LocalDimmingMode.Enabled => "service set-local-dimming enabled",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
