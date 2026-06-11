using System.ComponentModel;
using System.Diagnostics;
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

            try
            {
                if (preset.Settings.VisibleHud is not null and not VisibleHudMode.None)
                {
                    logger.Info(
                        "HUD overlays require a running PCVR application using the Meta/Oculus compositor; SteamVR-only rendering may not display them.");
                }

                commandFile = await GenerateCommandFileAsync(preset, cancellationToken);
                var startInfo = BuildStartInfo(commandFile);
                using var process = Process.Start(startInfo);

                if (process is null)
                {
                    return ApplyPresetResult.Fail(
                        "Failed to start OculusDebugToolCLI.exe.",
                        commandFile);
                }

                string? standardOutput = null;
                string? standardError = null;
                if (!settings.RequireElevationForCli)
                {
                    var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                    var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
                    await process.WaitForExitAsync(cancellationToken);
                    standardOutput = await outputTask;
                    standardError = await errorTask;
                }
                else
                {
                    await process.WaitForExitAsync(cancellationToken);
                }

                var output = JoinOutput(standardOutput, standardError);
                if (process.ExitCode == 0)
                {
                    logger.Info($"Preset applied successfully: {preset.Name}");
                    return ApplyPresetResult.Ok(commandFile, output);
                }

                var message = $"OculusDebugToolCLI.exe exited with code {process.ExitCode}.";
                logger.Error(message);
                return ApplyPresetResult.Fail(message, commandFile, output);
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
            {
                const string message = "The UAC elevation prompt was cancelled.";
                logger.Error(message);
                return ApplyPresetResult.Fail(message, commandFile);
            }
            catch (OperationCanceledException)
            {
                logger.Info($"Preset application cancelled: {preset.Name}");
                throw;
            }
            catch (Exception exception)
            {
                logger.Error($"Failed to apply preset {preset.Name}", exception);
                return ApplyPresetResult.Fail(exception.Message, commandFile);
            }
        }
        finally
        {
            _applyLock.Release();
        }
    }

    private ProcessStartInfo BuildStartInfo(string commandFile)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = settings.OculusDebugToolCliPath,
            UseShellExecute = settings.RequireElevationForCli,
            CreateNoWindow = !settings.RequireElevationForCli,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(commandFile);

        if (settings.RequireElevationForCli)
        {
            startInfo.Verb = "runas";
        }
        else
        {
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
        }

        return startInfo;
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

    private static string? JoinOutput(string? output, string? error)
    {
        var parts = new[] { output, error }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        var combined = string.Join(Environment.NewLine, parts);
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }
}
