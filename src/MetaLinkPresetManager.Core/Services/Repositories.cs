using System.Text.Json;
using System.Text.Json.Serialization;
using MetaLinkPresetManager.Core.Infrastructure;
using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.Core.Services;

public interface IAppSettingsRepository
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public interface IPresetRepository
{
    Task<PresetStore> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(PresetStore store, CancellationToken cancellationToken = default);
}

public static class JsonDefaults
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class JsonAppSettingsRepository(AppPaths paths) : IAppSettingsRepository
{
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        paths.EnsureCreated();

        if (!File.Exists(paths.SettingsFile))
        {
            var defaults = new AppSettings
            {
                OculusDebugToolCliPath = OculusCliLocator.FindBestPath()
            };
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(paths.SettingsFile);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(
            stream,
            JsonDefaults.Options,
            cancellationToken);

        if (settings is null)
        {
            throw new InvalidDataException("appsettings.json did not contain valid settings.");
        }

        if (string.IsNullOrWhiteSpace(settings.OculusDebugToolCliPath))
        {
            settings.OculusDebugToolCliPath = OculusCliLocator.FindBestPath();
        }

        settings.ProcessPollingIntervalSeconds = Math.Clamp(
            settings.ProcessPollingIntervalSeconds,
            1,
            60);
        return settings;
    }

    public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        paths.EnsureCreated();
        return AtomicJsonWriter.WriteAsync(paths.SettingsFile, settings, cancellationToken);
    }
}

public sealed class JsonPresetRepository(AppPaths paths) : IPresetRepository
{
    public async Task<PresetStore> LoadAsync(CancellationToken cancellationToken = default)
    {
        paths.EnsureCreated();

        if (!File.Exists(paths.PresetsFile))
        {
            var defaults = InitialPresets.Create();
            await SaveAsync(defaults, cancellationToken);
            return defaults;
        }

        await using var stream = File.OpenRead(paths.PresetsFile);
        var store = await JsonSerializer.DeserializeAsync<PresetStore>(
            stream,
            JsonDefaults.Options,
            cancellationToken);

        return store ?? throw new InvalidDataException("presets.json did not contain a valid preset store.");
    }

    public Task SaveAsync(PresetStore store, CancellationToken cancellationToken = default)
    {
        paths.EnsureCreated();
        return AtomicJsonWriter.WriteAsync(paths.PresetsFile, store, cancellationToken);
    }
}

internal static class AtomicJsonWriter
{
    public static async Task WriteAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        var temporaryPath = path + ".tmp";

        await using (var stream = new FileStream(
            temporaryPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                value,
                JsonDefaults.Options,
                cancellationToken);
        }

        File.Move(temporaryPath, path, true);
    }
}

internal static class InitialPresets
{
    public static PresetStore Create()
    {
        return new PresetStore
        {
            Presets =
            [
                new OculusPreset
                {
                    Id = "default-link",
                    Name = "Default Link",
                    ApplyAutomaticallyWhenProcessStarts = false,
                    RestoreDefaultPresetOnExit = false,
                    Settings = new OculusSettings
                    {
                        FovTanMultiplierHorizontal = 0,
                        FovTanMultiplierVertical = 0,
                        AswMode = AswMode.Auto,
                        EncodeBitrateMbps = 0,
                        EncodeResolutionWidth = 0,
                        LinkSharpening = LinkSharpeningMode.Normal,
                        LocalDimming = LocalDimmingMode.Enabled
                    }
                },
                new OculusPreset
                {
                    Id = "ams2-quest3-link",
                    Name = "AMS2 - Quest 3 Link",
                    ProcessName = "AMS2AVX.exe",
                    LaunchType = LaunchType.SteamUrl,
                    LaunchTarget = "steam://rungameid/1066890",
                    Settings = new OculusSettings
                    {
                        FovTanMultiplierHorizontal = 0.8m,
                        FovTanMultiplierVertical = 0.7m,
                        AswMode = AswMode.Off,
                        EncodeBitrateMbps = 500,
                        EncodeResolutionWidth = 0,
                        LinkSharpening = LinkSharpeningMode.Normal,
                        LocalDimming = LocalDimmingMode.Enabled
                    }
                }
            ]
        };
    }
}
