namespace MetaLinkPresetManager.Core.Models;

public sealed class PresetStore
{
    public List<OculusPreset> Presets { get; set; } = [];
}

public sealed class OculusPreset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? ProcessName { get; set; }
    public LaunchType LaunchType { get; set; }
    public string? LaunchTarget { get; set; }
    public bool ApplyAutomaticallyWhenProcessStarts { get; set; } = true;
    public bool RestoreDefaultPresetOnExit { get; set; } = true;
    public OculusSettings Settings { get; set; } = new();

    public OculusPreset Clone(bool createNewId = false)
    {
        return new OculusPreset
        {
            Id = createNewId ? Guid.NewGuid().ToString("N") : Id,
            Name = Name,
            Enabled = Enabled,
            ProcessName = ProcessName,
            LaunchType = LaunchType,
            LaunchTarget = LaunchTarget,
            ApplyAutomaticallyWhenProcessStarts = ApplyAutomaticallyWhenProcessStarts,
            RestoreDefaultPresetOnExit = RestoreDefaultPresetOnExit,
            Settings = Settings.Clone()
        };
    }

    public override string ToString()
    {
        return Name;
    }
}

public sealed class OculusSettings
{
    public decimal? FovTanMultiplierHorizontal { get; set; }
    public decimal? FovTanMultiplierVertical { get; set; }
    public AswMode? AswMode { get; set; }
    public int? EncodeBitrateMbps { get; set; }
    public int? EncodeResolutionWidth { get; set; }
    public LinkSharpeningMode? LinkSharpening { get; set; }
    public LocalDimmingMode? LocalDimming { get; set; }

    public OculusSettings Clone()
    {
        return new OculusSettings
        {
            FovTanMultiplierHorizontal = FovTanMultiplierHorizontal,
            FovTanMultiplierVertical = FovTanMultiplierVertical,
            AswMode = AswMode,
            EncodeBitrateMbps = EncodeBitrateMbps,
            EncodeResolutionWidth = EncodeResolutionWidth,
            LinkSharpening = LinkSharpening,
            LocalDimming = LocalDimming
        };
    }

    public bool HasAnyValue()
    {
        return FovTanMultiplierHorizontal.HasValue
            || FovTanMultiplierVertical.HasValue
            || AswMode.HasValue
            || EncodeBitrateMbps.HasValue
            || EncodeResolutionWidth.HasValue
            || LinkSharpening.HasValue
            || LocalDimming.HasValue;
    }
}

public enum LaunchType
{
    None,
    SteamUrl,
    Exe,
    Shortcut
}

public enum AswMode
{
    Auto,
    Off,
    Force45,
    Force45WithAsw,
    Disabled
}

public enum LinkSharpeningMode
{
    Default,
    Disabled,
    Normal,
    Quality
}

public enum LocalDimmingMode
{
    Default,
    Disabled,
    Enabled
}
