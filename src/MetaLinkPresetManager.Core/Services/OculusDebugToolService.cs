using MetaLinkPresetManager.Core.Infrastructure;
using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.Core.Services;

public interface IOculusDebugToolService
{
    bool CliExists();
    Task<string> GenerateCommandFileAsync(
        OculusPreset preset,
        CancellationToken cancellationToken = default);
    Task<ApplyPresetResult> ApplyPresetAsync(
        OculusPreset preset,
        CancellationToken cancellationToken = default);
}

public sealed class OculusDebugToolService(
    AppSettings settings,
    AppPaths paths,
    ICommandFileBuilder commandFileBuilder,
    IVideoCodecRegistryService videoCodecRegistryService,
    IOculusCliRunner cliRunner,
    IAppLogger logger) : IOculusDebugToolService
{
    private readonly SemaphoreSlim _applyLock = new(1, 1);

    public bool CliExists()
    {
        return File.Exists(settings.OculusDebugToolCliPath);
    }

    public async Task<string> GenerateCommandFileAsync(
        OculusPreset preset,
        CancellationToken cancellationToken = default)
    {
        var commands = commandFileBuilder.BuildCommands(preset);
        var safeName = MakeSafeFileName(preset.Name);
        var path = Path.Combine(paths.GeneratedFolder, safeName + ".txt");

        paths.EnsureCreated();
        await File.WriteAllLinesAsync(path, commands, cancellationToken);
        logger.Info($"Generated command file: {path}");
        foreach (var command in commands)
        {
            logger.Info($"Generated command: {command}");
        }
        return path;
    }

    public async Task<ApplyPresetResult> ApplyPresetAsync(
        OculusPreset preset,
        CancellationToken cancellationToken = default)
    {
        var errors = PresetValidator.ValidateForApply(
            preset,
            settings.OculusDebugToolCliPath);
        if (errors.Count > 0)
        {
            return ApplyPresetResult.Fail(string.Join(Environment.NewLine, errors));
        }

        await _applyLock.WaitAsync(cancellationToken);

        try
        {
            logger.Info($"Applying preset: {preset.Name}");
            string? commandFile = null;
            RegistryValueSnapshot? codecSnapshot = null;

            try
            {
                if (preset.Settings.VideoCodec.HasValue)
                {
                    codecSnapshot = videoCodecRegistryService.Capture();
                    videoCodecRegistryService.Apply(
                        preset.Settings.VideoCodec.Value);
                }

                if (!preset.Settings.HasCliApplicableValue())
                {
                    logger.Info($"Preset applied successfully: {preset.Name}");
                    return ApplyPresetResult.Ok();
                }

                if (preset.Settings.VisibleHud is not null and not VisibleHudMode.None)
                {
                    logger.Info(
                        "HUD overlays require a running PCVR application using the Meta/Oculus compositor; SteamVR-only rendering may not display them.");
                }

                commandFile = await GenerateCommandFileAsync(preset, cancellationToken);
                var cliResult = await cliRunner.RunAsync(
                    settings.OculusDebugToolCliPath,
                    commandFile,
                    settings.RequireElevationForCli,
                    cancellationToken);
                if (cliResult.Succeeded)
                {
                    logger.Info($"Preset applied successfully: {preset.Name}");
                    return ApplyPresetResult.Ok(
                        commandFile,
                        cliResult.ProcessOutput);
                }

                var message = cliResult.ErrorMessage
                    ?? "OculusDebugToolCLI.exe could not apply the preset.";
                logger.Error(message);
                var rollbackError = TryRestoreCodec(codecSnapshot);
                return ApplyPresetResult.Fail(
                    AppendRollbackError(message, rollbackError),
                    commandFile,
                    cliResult.ProcessOutput);
            }
            catch (OperationCanceledException)
            {
                TryRestoreCodec(codecSnapshot);
                logger.Info($"Preset application cancelled: {preset.Name}");
                throw;
            }
            catch (Exception exception)
            {
                logger.Error($"Failed to apply preset {preset.Name}", exception);
                var rollbackError = TryRestoreCodec(codecSnapshot);
                return ApplyPresetResult.Fail(
                    AppendRollbackError(exception.Message, rollbackError),
                    commandFile);
            }
        }
        finally
        {
            _applyLock.Release();
        }
    }

    private string? TryRestoreCodec(RegistryValueSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        try
        {
            videoCodecRegistryService.Restore(snapshot);
            return null;
        }
        catch (Exception exception)
        {
            logger.Error(
                "Failed to restore the previous Video Codec registry state",
                exception);
            return exception.Message;
        }
    }

    private static string MakeSafeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var safeCharacters = value
            .Trim()
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();
        var safeName = new string(safeCharacters);
        return string.IsNullOrWhiteSpace(safeName) ? "Preset" : safeName;
    }

    private static string AppendRollbackError(
        string message,
        string? rollbackError)
    {
        return rollbackError is null
            ? message
            : $"{message}{Environment.NewLine}Video Codec rollback also failed: {rollbackError}";
    }
}
