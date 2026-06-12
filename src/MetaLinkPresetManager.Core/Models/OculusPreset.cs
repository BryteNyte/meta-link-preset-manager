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
    public decimal? PixelsPerDisplayPixelOverride { get; set; }
    public bool? ForceMipmapGenerationOnAllLayers { get; set; }
    public bool? OffsetMipmapBiasOnAllLayers { get; set; }
    public bool? UseFovStencil { get; set; }
    public bool? BypassProximitySensorCheck { get; set; }
    public bool? AdaptiveGpuPerformanceScale { get; set; }
    public PcAswMode? PcAsynchronousSpacewarp { get; set; }
    public bool? FrameDropIndicator { get; set; }
    public DebugHmdType? DebugHmdType { get; set; }
    public bool? PoseInjection { get; set; }
    public DistortionCurvatureMode? DistortionCurvature { get; set; }
    public VideoCodecMode? VideoCodec { get; set; }
    public bool? SlicedEncoding { get; set; }
    public bool? EncodeDynamicBitrate { get; set; }
    public int? DynamicBitrateMax { get; set; }
    public int? DynamicBitrateOffset { get; set; }
    public int? EncodeBitrateMbps { get; set; }
    public int? EncodeResolutionWidth { get; set; }
    public LinkSharpeningMode? LinkSharpening { get; set; }
    public LocalDimmingMode? LocalDimming { get; set; }
    public VisibleHudMode? VisibleHud { get; set; }
    public PerformanceHudMode? PerformanceHud { get; set; }
    public StereoDebugHudMode? StereoDebugHud { get; set; }
    public LayerHudMode? LayerHud { get; set; }
    public bool? LostFrameCapture { get; set; }

    public OculusSettings Clone()
    {
        return new OculusSettings
        {
            FovTanMultiplierHorizontal = FovTanMultiplierHorizontal,
            FovTanMultiplierVertical = FovTanMultiplierVertical,
            AswMode = AswMode,
            PixelsPerDisplayPixelOverride = PixelsPerDisplayPixelOverride,
            ForceMipmapGenerationOnAllLayers = ForceMipmapGenerationOnAllLayers,
            OffsetMipmapBiasOnAllLayers = OffsetMipmapBiasOnAllLayers,
            UseFovStencil = UseFovStencil,
            BypassProximitySensorCheck = BypassProximitySensorCheck,
            AdaptiveGpuPerformanceScale = AdaptiveGpuPerformanceScale,
            PcAsynchronousSpacewarp = PcAsynchronousSpacewarp,
            FrameDropIndicator = FrameDropIndicator,
            DebugHmdType = DebugHmdType,
            PoseInjection = PoseInjection,
            DistortionCurvature = DistortionCurvature,
            VideoCodec = VideoCodec,
            SlicedEncoding = SlicedEncoding,
            EncodeDynamicBitrate = EncodeDynamicBitrate,
            DynamicBitrateMax = DynamicBitrateMax,
            DynamicBitrateOffset = DynamicBitrateOffset,
            EncodeBitrateMbps = EncodeBitrateMbps,
            EncodeResolutionWidth = EncodeResolutionWidth,
            LinkSharpening = LinkSharpening,
            LocalDimming = LocalDimming,
            VisibleHud = VisibleHud,
            PerformanceHud = PerformanceHud,
            StereoDebugHud = StereoDebugHud,
            LayerHud = LayerHud,
            LostFrameCapture = LostFrameCapture
        };
    }

    public bool HasAnyValue()
    {
        return FovTanMultiplierHorizontal.HasValue
            || FovTanMultiplierVertical.HasValue
            || AswMode.HasValue
            || PixelsPerDisplayPixelOverride.HasValue
            || ForceMipmapGenerationOnAllLayers.HasValue
            || OffsetMipmapBiasOnAllLayers.HasValue
            || UseFovStencil.HasValue
            || BypassProximitySensorCheck.HasValue
            || AdaptiveGpuPerformanceScale.HasValue
            || PcAsynchronousSpacewarp.HasValue
            || FrameDropIndicator.HasValue
            || DebugHmdType.HasValue
            || PoseInjection.HasValue
            || DistortionCurvature.HasValue
            || VideoCodec.HasValue
            || SlicedEncoding.HasValue
            || EncodeDynamicBitrate.HasValue
            || DynamicBitrateMax.HasValue
            || DynamicBitrateOffset.HasValue
            || EncodeBitrateMbps.HasValue
            || EncodeResolutionWidth.HasValue
            || LinkSharpening.HasValue
            || LocalDimming.HasValue
            || VisibleHud.HasValue
            || PerformanceHud.HasValue
            || StereoDebugHud.HasValue
            || LayerHud.HasValue
            || LostFrameCapture.HasValue;
    }

    public bool HasCliApplicableValue()
    {
        return FovTanMultiplierHorizontal.HasValue
            || FovTanMultiplierVertical.HasValue
            || AswMode.HasValue
            || PixelsPerDisplayPixelOverride.HasValue
            || ForceMipmapGenerationOnAllLayers.HasValue
            || OffsetMipmapBiasOnAllLayers.HasValue
            || UseFovStencil.HasValue
            || AdaptiveGpuPerformanceScale.HasValue
            || FrameDropIndicator.HasValue
            || PoseInjection.HasValue
            || EncodeBitrateMbps.HasValue
            || EncodeResolutionWidth.HasValue
            || LinkSharpening.HasValue
            || LocalDimming.HasValue
            || VisibleHud.HasValue;
    }

    public bool HasRegistryApplicableValue()
    {
        return VideoCodec.HasValue && VideoCodec != VideoCodecMode.Av1;
    }
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

public enum PcAswMode
{
    Auto,
    Disabled,
    Force45,
    Force45WithAsw
}

public enum DebugHmdType
{
    None,
    Rift,
    RiftS,
    Quest,
    Quest2,
    QuestPro,
    Quest3
}

public enum DistortionCurvatureMode
{
    Default,
    Low,
    High
}

public enum VideoCodecMode
{
    Default,
    H264,
    H265,
    Av1
}

public enum VisibleHudMode
{
    None,
    Performance,
    StereoDebug,
    Layer
}

public enum PerformanceHudMode
{
    None,
    PerformanceSummary,
    LatencyTiming,
    AppRenderTiming,
    CompositorRenderTiming,
    VersionInfo,
    AswStats
}

public enum StereoDebugHudMode
{
    None,
    Quad,
    QuadWithCrosshair,
    CrosshairAtInfinity
}

public enum LayerHudMode
{
    None,
    LayerInfo,
    ShowAllLayers
}
