namespace MetaLinkPresetManager.Core.Models;

public sealed class AppSettings
{
    public string OculusDebugToolCliPath { get; set; } = string.Empty;
    public bool RequireElevationForCli { get; set; } = true;
    public int ProcessPollingIntervalSeconds { get; set; } = 2;
    public bool RestoreDefaultPresetOnAppExit { get; set; }
    public string? DefaultPresetId { get; set; } = "default-link";

    public AppSettings Clone()
    {
        return new AppSettings
        {
            OculusDebugToolCliPath = OculusDebugToolCliPath,
            RequireElevationForCli = RequireElevationForCli,
            ProcessPollingIntervalSeconds = ProcessPollingIntervalSeconds,
            RestoreDefaultPresetOnAppExit = RestoreDefaultPresetOnAppExit,
            DefaultPresetId = DefaultPresetId
        };
    }
}
