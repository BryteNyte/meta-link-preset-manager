namespace MetaLinkPresetManager.Core.Infrastructure;

public sealed class AppPaths
{
    public AppPaths(string? rootFolder = null)
    {
        RootFolder = rootFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MetaLinkPresetManager");
    }

    public string RootFolder { get; }
    public string SettingsFile => Path.Combine(RootFolder, "appsettings.json");
    public string PresetsFile => Path.Combine(RootFolder, "presets.json");
    public string GeneratedFolder => Path.Combine(RootFolder, "generated");
    public string LogsFolder => Path.Combine(RootFolder, "logs");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootFolder);
        Directory.CreateDirectory(GeneratedFolder);
        Directory.CreateDirectory(LogsFolder);
    }
}
