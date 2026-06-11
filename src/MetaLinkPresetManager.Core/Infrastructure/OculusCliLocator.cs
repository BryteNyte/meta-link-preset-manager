namespace MetaLinkPresetManager.Core.Infrastructure;

public static class OculusCliLocator
{
    private static readonly string[] KnownPaths =
    [
        @"C:\Program Files\Meta Horizon\Support\oculus-diagnostics\OculusDebugToolCLI.exe",
        @"C:\Program Files\Oculus\Support\oculus-diagnostics\OculusDebugToolCLI.exe",
        @"C:\Program Files\Meta\Quest\Support\oculus-diagnostics\OculusDebugToolCLI.exe"
    ];

    public static string FindBestPath()
    {
        return KnownPaths.FirstOrDefault(File.Exists) ?? KnownPaths[0];
    }
}
