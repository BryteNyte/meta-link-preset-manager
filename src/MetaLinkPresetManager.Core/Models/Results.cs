namespace MetaLinkPresetManager.Core.Models;

public sealed class ApplyPresetResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CommandFilePath { get; init; }
    public string? ProcessOutput { get; init; }

    public static ApplyPresetResult Ok(
        string? commandFilePath = null,
        string? processOutput = null)
    {
        return new ApplyPresetResult
        {
            Succeeded = true,
            CommandFilePath = commandFilePath,
            ProcessOutput = processOutput
        };
    }

    public static ApplyPresetResult Fail(
        string errorMessage,
        string? commandFilePath = null,
        string? processOutput = null)
    {
        return new ApplyPresetResult
        {
            ErrorMessage = errorMessage,
            CommandFilePath = commandFilePath,
            ProcessOutput = processOutput
        };
    }
}

public sealed class WatchedProcessEventArgs(OculusPreset preset) : EventArgs
{
    public OculusPreset Preset { get; } = preset;
}
