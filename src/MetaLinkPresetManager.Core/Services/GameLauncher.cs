using System.Diagnostics;
using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.Core.Services;

public interface IGameLauncher
{
    Task<LaunchResult> LaunchAsync(
        OculusPreset preset,
        CancellationToken cancellationToken = default);
}

public sealed class GameLauncher(IAppLogger logger) : IGameLauncher
{
    public Task<LaunchResult> LaunchAsync(
        OculusPreset preset,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var error = ValidateTarget(preset);
        if (error is not null)
        {
            return Task.FromResult(LaunchResult.Fail(error));
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = preset.LaunchTarget!,
                UseShellExecute = true
            });
            logger.Info($"Launch target started for preset: {preset.Name}");
            return Task.FromResult(LaunchResult.Ok());
        }
        catch (Exception exception)
        {
            logger.Error($"Failed to launch target for preset {preset.Name}", exception);
            return Task.FromResult(LaunchResult.Fail(exception.Message));
        }
    }

    private static string? ValidateTarget(OculusPreset preset)
    {
        if (preset.LaunchType == LaunchType.None
            || string.IsNullOrWhiteSpace(preset.LaunchTarget))
        {
            return "No launch target is configured for this preset.";
        }

        if (preset.LaunchType == LaunchType.SteamUrl
            && !preset.LaunchTarget.StartsWith(
                "steam://",
                StringComparison.OrdinalIgnoreCase))
        {
            return "Steam launch targets must start with steam://.";
        }

        if (preset.LaunchType is LaunchType.Exe or LaunchType.Shortcut
            && !File.Exists(preset.LaunchTarget))
        {
            return $"Launch target was not found: {preset.LaunchTarget}";
        }

        return null;
    }
}
