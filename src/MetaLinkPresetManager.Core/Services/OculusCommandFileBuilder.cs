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

        if (settings.PixelsPerDisplayPixelOverride.HasValue)
        {
            commands.Add(string.Format(
                CultureInfo.InvariantCulture,
                "service set-pixels-per-display-pixel-override {0:0.00}",
                settings.PixelsPerDisplayPixelOverride.Value));
        }

        if (settings.ForceMipmapGenerationOnAllLayers.HasValue)
        {
            commands.Add(
                $"service set-force-mip-gen-on-all-layers {ToCliBoolean(settings.ForceMipmapGenerationOnAllLayers.Value)}");
        }

        if (settings.OffsetMipmapBiasOnAllLayers.HasValue)
        {
            commands.Add(
                $"service set-offset-mip-bias-on-all-layers {ToCliBoolean(settings.OffsetMipmapBiasOnAllLayers.Value)}");
        }

        if (settings.UseFovStencil.HasValue)
        {
            commands.Add(
                $"service set-use-fov-stencil {ToCliBoolean(settings.UseFovStencil.Value)}");
        }

        if (settings.AdaptiveGpuPerformanceScale.HasValue)
        {
            commands.Add(
                $"service enable-adaptive-gpu-perf-scale {ToCliBoolean(settings.AdaptiveGpuPerformanceScale.Value)}");
        }

        if (settings.FrameDropIndicator.HasValue)
        {
            commands.Add(
                $"server:FrameDropHUDEnabled {ToCliBoolean(settings.FrameDropIndicator.Value)}");
        }

        if (settings.PoseInjection.HasValue)
        {
            commands.Add($"service set-pose-injection {(settings.PoseInjection.Value ? 1 : 0)}");
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

        AddHudCommands(commands, settings);

        commands.Add("exit");
        return commands;
    }

    private static void AddHudCommands(
        ICollection<string> commands,
        OculusSettings settings)
    {
        if (!settings.VisibleHud.HasValue)
        {
            return;
        }

        commands.Add("perfhud reset");
        commands.Add("stereohud set-mode 0");
        commands.Add("layerhud reset");

        switch (settings.VisibleHud.Value)
        {
            case VisibleHudMode.None:
                return;
            case VisibleHudMode.Performance:
                commands.Add(
                    $"perfhud set-mode {MapPerformanceHudMode(RequirePerformanceHudMode(settings))}");
                return;
            case VisibleHudMode.StereoDebug:
                commands.Add(
                    $"stereohud set-mode {MapStereoDebugHudMode(RequireStereoDebugHudMode(settings))}");
                return;
            case VisibleHudMode.Layer:
                commands.Add("layerhud set-mode 1");
                commands.Add(
                    $"layerhud show-all-layers {ToCliBoolean(settings.LayerHud == LayerHudMode.ShowAllLayers)}");
                return;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(settings.VisibleHud),
                    settings.VisibleHud,
                    null);
        }
    }

    private static PerformanceHudMode RequirePerformanceHudMode(
        OculusSettings settings)
    {
        return settings.PerformanceHud is null or PerformanceHudMode.None
            ? throw new InvalidOperationException(
                "Performance HUD requires a non-None performance mode.")
            : settings.PerformanceHud.Value;
    }

    private static StereoDebugHudMode RequireStereoDebugHudMode(
        OculusSettings settings)
    {
        return settings.StereoDebugHud is null or StereoDebugHudMode.None
            ? throw new InvalidOperationException(
                "Stereo Debug HUD requires a non-None stereo mode.")
            : settings.StereoDebugHud.Value;
    }

    private static string ToCliBoolean(bool value)
    {
        return value ? "true" : "false";
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

    private static int MapPerformanceHudMode(PerformanceHudMode mode)
    {
        return mode switch
        {
            PerformanceHudMode.None => 0,
            PerformanceHudMode.PerformanceSummary => 1,
            PerformanceHudMode.LatencyTiming => 2,
            PerformanceHudMode.AppRenderTiming => 3,
            PerformanceHudMode.CompositorRenderTiming => 4,
            PerformanceHudMode.VersionInfo => 5,
            PerformanceHudMode.AswStats => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private static int MapStereoDebugHudMode(StereoDebugHudMode mode)
    {
        return mode switch
        {
            StereoDebugHudMode.None => 0,
            StereoDebugHudMode.Quad => 1,
            StereoDebugHudMode.QuadWithCrosshair => 2,
            StereoDebugHudMode.CrosshairAtInfinity => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
