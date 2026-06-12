using System.ComponentModel;
using System.Diagnostics;

namespace MetaLinkPresetManager.Core.Services;

public sealed record CliExecutionResult(
    bool Succeeded,
    string? ErrorMessage = null,
    string? ProcessOutput = null);

public interface IOculusCliRunner
{
    Task<CliExecutionResult> RunAsync(
        string cliPath,
        string commandFile,
        bool requireElevation,
        CancellationToken cancellationToken = default);
}

public sealed class OculusCliRunner : IOculusCliRunner
{
    public async Task<CliExecutionResult> RunAsync(
        string cliPath,
        string commandFile,
        bool requireElevation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startInfo = BuildStartInfo(
                cliPath,
                commandFile,
                requireElevation);
            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new CliExecutionResult(
                    false,
                    "Failed to start OculusDebugToolCLI.exe.");
            }

            string? standardOutput = null;
            string? standardError = null;
            if (!requireElevation)
            {
                var outputTask = process.StandardOutput.ReadToEndAsync(
                    cancellationToken);
                var errorTask = process.StandardError.ReadToEndAsync(
                    cancellationToken);
                await process.WaitForExitAsync(cancellationToken);
                standardOutput = await outputTask;
                standardError = await errorTask;
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken);
            }

            var output = JoinOutput(standardOutput, standardError);
            return process.ExitCode == 0
                ? new CliExecutionResult(true, ProcessOutput: output)
                : new CliExecutionResult(
                    false,
                    $"OculusDebugToolCLI.exe exited with code {process.ExitCode}.",
                    output);
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            return new CliExecutionResult(
                false,
                "The UAC elevation prompt was cancelled.");
        }
    }

    private static ProcessStartInfo BuildStartInfo(
        string cliPath,
        string commandFile,
        bool requireElevation)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            UseShellExecute = requireElevation,
            CreateNoWindow = !requireElevation,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add(commandFile);

        if (requireElevation)
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

    private static string? JoinOutput(string? output, string? error)
    {
        var parts = new[] { output, error }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());
        var combined = string.Join(Environment.NewLine, parts);
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }
}
