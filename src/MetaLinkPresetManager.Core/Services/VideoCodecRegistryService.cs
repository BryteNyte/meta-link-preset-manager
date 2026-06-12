using Microsoft.Win32;
using MetaLinkPresetManager.Core.Models;
using System.Runtime.Versioning;

namespace MetaLinkPresetManager.Core.Services;

public sealed record RegistryValueSnapshot(
    bool Exists,
    object? Value,
    int ValueKind);

public interface IRegistryValueStore
{
    RegistryValueSnapshot Capture();
    void SetDword(int value);
    void Delete();
    void Restore(RegistryValueSnapshot snapshot);
}

[SupportedOSPlatform("windows")]
public sealed class MetaVideoCodecRegistryStore : IRegistryValueStore
{
    private const string KeyPath = @"Software\Oculus\RemoteHeadset";
    private const string ValueName = "HEVC";

    public RegistryValueSnapshot Capture()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
        if (key is null
            || !key.GetValueNames().Contains(ValueName, StringComparer.Ordinal))
        {
            return new RegistryValueSnapshot(false, null, -1);
        }

        return new RegistryValueSnapshot(
            true,
            key.GetValue(ValueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames),
            (int)key.GetValueKind(ValueName));
    }

    public void SetDword(int value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true);
        key.SetValue(ValueName, value, RegistryValueKind.DWord);
    }

    public void Delete()
    {
        using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
        key?.DeleteValue(ValueName, false);
    }

    public void Restore(RegistryValueSnapshot snapshot)
    {
        if (!snapshot.Exists)
        {
            Delete();
            return;
        }

        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true);
        key.SetValue(
            ValueName,
            snapshot.Value!,
            (RegistryValueKind)snapshot.ValueKind);
    }
}

public interface IVideoCodecRegistryService
{
    RegistryValueSnapshot Capture();
    void Apply(VideoCodecMode codec);
    void Restore(RegistryValueSnapshot snapshot);
}

public sealed class VideoCodecRegistryService(
    IRegistryValueStore store,
    IAppLogger logger) : IVideoCodecRegistryService
{
    public RegistryValueSnapshot Capture()
    {
        var snapshot = store.Capture();
        logger.Info(
            snapshot.Exists
                ? "Captured existing registry-backed Video Codec override."
                : "Captured Video Codec state with no registry override.");
        return snapshot;
    }

    public void Apply(VideoCodecMode codec)
    {
        switch (codec)
        {
            case VideoCodecMode.Default:
                store.Delete();
                logger.Info(
                    "Applied registry-backed Video Codec: System Default (deleted HEVC override).");
                return;
            case VideoCodecMode.H264:
                store.SetDword(0);
                logger.Info("Applied registry-backed Video Codec: H.264 (HEVC=0).");
                return;
            case VideoCodecMode.H265:
                store.SetDword(1);
                logger.Info("Applied registry-backed Video Codec: H.265 (HEVC=1).");
                return;
            case VideoCodecMode.Av1:
                throw new NotSupportedException(
                    "AV1 is not supported by the installed Meta Debug Tool.");
            default:
                throw new ArgumentOutOfRangeException(nameof(codec), codec, null);
        }
    }

    public void Restore(RegistryValueSnapshot snapshot)
    {
        store.Restore(snapshot);
        logger.Info("Restored the previous registry-backed Video Codec state.");
    }
}
