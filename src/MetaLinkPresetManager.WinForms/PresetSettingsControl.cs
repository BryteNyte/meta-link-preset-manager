using MetaLinkPresetManager.Core.Models;
using System.Text.RegularExpressions;

namespace MetaLinkPresetManager.WinForms;

public sealed class PresetSettingsControl : UserControl
{
    private readonly CheckBox _includeFov = new();
    private readonly NumericUpDown _fovHorizontal =
        CreateNumeric(0, 10, 2, 0.01m);
    private readonly NumericUpDown _fovVertical = CreateNumeric(0, 10, 2, 0.01m);
    private readonly OptionalEnumRow<AswMode> _asw = new("ASW mode");

    private readonly OptionalEnumRow<DistortionCurvatureMode> _distortion =
        new("Distortion curvature (stored only)");
    private readonly OptionalEnumRow<VideoCodecMode> _videoCodec =
        new("Video codec (registry-backed)", FormatVideoCodec);
    private readonly OptionalBooleanRow _slicedEncoding =
        new("Sliced encoding (stored only)");
    private readonly OptionalBooleanRow _dynamicBitrate =
        new("Encode dynamic bitrate (stored only)");
    private readonly OptionalNumericRow _dynamicBitrateMax =
        new("Dynamic bitrate max (stored only)", 0, 2000);
    private readonly OptionalNumericRow _dynamicBitrateOffset =
        new("Dynamic bitrate offset (stored only)", 0, 2000);
    private readonly OptionalNumericRow _encodeBitrate =
        new("Encode bitrate Mbps", 0, 2000);
    private readonly OptionalNumericRow _encodeResolution =
        new("Encode resolution width", 0, 10000);
    private readonly OptionalEnumRow<LinkSharpeningMode> _linkSharpening =
        new("Link sharpening");
    private readonly OptionalEnumRow<LocalDimmingMode> _localDimming =
        new("Local dimming");

    private readonly OptionalNumericRow _pixelsPerDisplayPixel =
        new("Pixels per display pixel override", 0, 5, 2, 0.05m);
    private readonly OptionalBooleanRow _forceMipmap =
        new("Force mipmap generation on all layers");
    private readonly OptionalBooleanRow _offsetMipmapBias =
        new("Offset mipmap bias on all layers");
    private readonly OptionalBooleanRow _useFovStencil = new("Use FOV stencil");
    private readonly OptionalBooleanRow _bypassProximity =
        new("Bypass proximity sensor check (stored only)");
    private readonly OptionalBooleanRow _adaptiveGpuScale =
        new("Adaptive GPU performance scale");
    private readonly OptionalEnumRow<PcAswMode> _pcAsw =
        new("(PC) Asynchronous Spacewarp (stored only)");
    private readonly OptionalBooleanRow _frameDropIndicator =
        new("Frame drop indicator");
    private readonly OptionalEnumRow<DebugHmdType> _debugHmd =
        new("Debug HMD type (stored only)");
    private readonly OptionalBooleanRow _poseInjection = new("Pose injection");

    private readonly HudSettingsEditor _hud = new();

    private readonly OptionalBooleanRow _lostFrameCapture =
        new("Lost frame capture (stored only)");

    public PresetSettingsControl()
    {
        Dock = DockStyle.Fill;
        BuildLayout();
    }

    public void LoadSettings(OculusSettings settings)
    {
        _includeFov.Checked = settings.FovTanMultiplierHorizontal.HasValue;
        if (settings.FovTanMultiplierHorizontal.HasValue)
        {
            _fovHorizontal.Value = Clamp(
                settings.FovTanMultiplierHorizontal.Value,
                _fovHorizontal);
        }
        if (settings.FovTanMultiplierVertical.HasValue)
        {
            _fovVertical.Value = Clamp(
                settings.FovTanMultiplierVertical.Value,
                _fovVertical);
        }
        UpdateFovState();
        _asw.SetValue(settings.AswMode);

        _distortion.SetValue(settings.DistortionCurvature);
        _videoCodec.SetValue(settings.VideoCodec);
        _slicedEncoding.SetValue(settings.SlicedEncoding);
        _dynamicBitrate.SetValue(settings.EncodeDynamicBitrate);
        _dynamicBitrateMax.SetValue(settings.DynamicBitrateMax);
        _dynamicBitrateOffset.SetValue(settings.DynamicBitrateOffset);
        _encodeBitrate.SetValue(settings.EncodeBitrateMbps);
        _encodeResolution.SetValue(settings.EncodeResolutionWidth);
        _linkSharpening.SetValue(settings.LinkSharpening);
        _localDimming.SetValue(settings.LocalDimming);

        _pixelsPerDisplayPixel.SetValue(settings.PixelsPerDisplayPixelOverride);
        _forceMipmap.SetValue(settings.ForceMipmapGenerationOnAllLayers);
        _offsetMipmapBias.SetValue(settings.OffsetMipmapBiasOnAllLayers);
        _useFovStencil.SetValue(settings.UseFovStencil);
        _bypassProximity.SetValue(settings.BypassProximitySensorCheck);
        _adaptiveGpuScale.SetValue(settings.AdaptiveGpuPerformanceScale);
        _pcAsw.SetValue(settings.PcAsynchronousSpacewarp);
        _frameDropIndicator.SetValue(settings.FrameDropIndicator);
        _debugHmd.SetValue(settings.DebugHmdType);
        _poseInjection.SetValue(settings.PoseInjection);

        _hud.LoadSettings(settings);
        _lostFrameCapture.SetValue(settings.LostFrameCapture);
    }

    public OculusSettings ReadSettings()
    {
        var settings = new OculusSettings
        {
            FovTanMultiplierHorizontal = _includeFov.Checked
                ? _fovHorizontal.Value
                : null,
            FovTanMultiplierVertical = _includeFov.Checked
                ? _fovVertical.Value
                : null,
            AswMode = _asw.GetValue(),
            DistortionCurvature = _distortion.GetValue(),
            VideoCodec = _videoCodec.GetValue(),
            SlicedEncoding = _slicedEncoding.GetValue(),
            EncodeDynamicBitrate = _dynamicBitrate.GetValue(),
            DynamicBitrateMax = _dynamicBitrateMax.GetIntValue(),
            DynamicBitrateOffset = _dynamicBitrateOffset.GetIntValue(),
            EncodeBitrateMbps = _encodeBitrate.GetIntValue(),
            EncodeResolutionWidth = _encodeResolution.GetIntValue(),
            LinkSharpening = _linkSharpening.GetValue(),
            LocalDimming = _localDimming.GetValue(),
            PixelsPerDisplayPixelOverride = _pixelsPerDisplayPixel.GetDecimalValue(),
            ForceMipmapGenerationOnAllLayers = _forceMipmap.GetValue(),
            OffsetMipmapBiasOnAllLayers = _offsetMipmapBias.GetValue(),
            UseFovStencil = _useFovStencil.GetValue(),
            BypassProximitySensorCheck = _bypassProximity.GetValue(),
            AdaptiveGpuPerformanceScale = _adaptiveGpuScale.GetValue(),
            PcAsynchronousSpacewarp = _pcAsw.GetValue(),
            FrameDropIndicator = _frameDropIndicator.GetValue(),
            DebugHmdType = _debugHmd.GetValue(),
            PoseInjection = _poseInjection.GetValue(),
            LostFrameCapture = _lostFrameCapture.GetValue()
        };
        _hud.WriteSettings(settings);
        return settings;
    }

    private void BuildLayout()
    {
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateTab(
            "Common",
            CreateFovRow(),
            _asw));
        tabs.TabPages.Add(CreateTab(
            "Oculus Link",
            CreateCompatibilityNote(),
            _distortion,
            _videoCodec,
            _slicedEncoding,
            _dynamicBitrate,
            _dynamicBitrateMax,
            _dynamicBitrateOffset,
            _encodeBitrate,
            _encodeResolution,
            _linkSharpening,
            _localDimming));
        tabs.TabPages.Add(CreateTab(
            "Service",
            CreateCompatibilityNote(),
            _pixelsPerDisplayPixel,
            _forceMipmap,
            _offsetMipmapBias,
            _useFovStencil,
            _bypassProximity,
            _adaptiveGpuScale,
            _pcAsw,
            _frameDropIndicator,
            _debugHmd,
            _poseInjection));
        tabs.TabPages.Add(CreateTab(
            "HUD",
            new Label
            {
                Text = "HUD overlays appear over a running PCVR application using wired Link or Air Link. They do not appear in standalone Quest Home.",
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Padding = new Padding(6)
            },
            _hud));
        tabs.TabPages.Add(CreateTab(
            "Advanced",
            CreateCompatibilityNote(),
            _lostFrameCapture,
            new Label
            {
                Text = "Layer property sub-options will be added after their CLI syntax is verified.",
                AutoSize = true,
                Padding = new Padding(6)
            }));
        Controls.Add(tabs);
    }

    private Control CreateFovRow()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 6,
            Padding = new Padding(0, 2, 0, 2)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        _includeFov.AutoSize = true;
        _includeFov.Anchor = AnchorStyles.Left;
        _includeFov.CheckedChanged += (_, _) => UpdateFovState();
        panel.Controls.Add(_includeFov, 0, 0);
        panel.Controls.Add(new Label
        {
            Text = "FOV-Tangent Multiplier",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 6, 12, 3)
        }, 1, 0);
        panel.Controls.Add(new Label
        {
            Text = "Horizontal",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 6, 6, 3)
        }, 2, 0);
        _fovHorizontal.Dock = DockStyle.Fill;
        panel.Controls.Add(_fovHorizontal, 3, 0);
        panel.Controls.Add(new Label
        {
            Text = "Vertical",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(12, 6, 6, 3)
        }, 4, 0);
        _fovVertical.Dock = DockStyle.Fill;
        panel.Controls.Add(_fovVertical, 5, 0);
        UpdateFovState();
        return panel;
    }

    private void UpdateFovState()
    {
        _fovHorizontal.Enabled = _includeFov.Checked;
        _fovVertical.Enabled = _includeFov.Checked;
    }

    private static string FormatVideoCodec(VideoCodecMode codec)
    {
        return codec switch
        {
            VideoCodecMode.Default => "System Default",
            VideoCodecMode.H264 => "H.264",
            VideoCodecMode.H265 => "H.265",
            VideoCodecMode.Av1 => "AV1 (unsupported)",
            _ => codec.ToString()
        };
    }

    private static TabPage CreateTab(string title, params Control[] controls)
    {
        var page = new TabPage(title);
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8)
        };
        panel.SizeChanged += (_, _) =>
        {
            foreach (Control control in panel.Controls)
            {
                control.Width = Math.Max(300, panel.ClientSize.Width - 28);
            }
        };
        panel.Controls.AddRange(controls);
        page.Controls.Add(panel);
        return page;
    }

    private static Label CreateCompatibilityNote()
    {
        return new Label
        {
            Text = "Fields marked \"stored only\" are saved in presets but are not sent to the CLI. Video Codec is applied separately through Meta's per-user registry override.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(6)
        };
    }

    private static NumericUpDown CreateNumeric(
        decimal minimum,
        decimal maximum,
        int decimalPlaces = 0,
        decimal increment = 1)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            DecimalPlaces = decimalPlaces,
            Increment = increment,
            ThousandsSeparator = decimalPlaces == 0
        };
    }

    private static decimal Clamp(decimal value, NumericUpDown control)
    {
        return Math.Clamp(value, control.Minimum, control.Maximum);
    }

    private sealed class OptionalNumericRow : UserControl
    {
        private readonly CheckBox _include = new();
        private readonly NumericUpDown _value;

        public OptionalNumericRow(
            string label,
            decimal minimum,
            decimal maximum,
            int decimalPlaces = 0,
            decimal increment = 1)
        {
            AutoSize = true;
            Dock = DockStyle.Top;
            _include.Text = label;
            _include.AutoSize = true;
            _value = CreateNumeric(minimum, maximum, decimalPlaces, increment);
            BuildRow(_include, _value);
            _include.CheckedChanged += (_, _) =>
            {
                _value.Enabled = _include.Checked;
                IncludeChanged?.Invoke(this, EventArgs.Empty);
            };
            _value.Enabled = false;
        }

        public event EventHandler? IncludeChanged;
        public bool Included => _include.Checked;

        public void SetValue(decimal? value)
        {
            _include.Checked = value.HasValue;
            if (value.HasValue)
            {
                _value.Value = Clamp(value.Value, _value);
            }
        }

        public void SetValue(int? value)
        {
            SetValue(value.HasValue ? (decimal?)value.Value : null);
        }

        public decimal? GetDecimalValue()
        {
            return Included ? _value.Value : null;
        }

        public int? GetIntValue()
        {
            return Included ? decimal.ToInt32(_value.Value) : null;
        }

        private void BuildRow(CheckBox include, Control value)
        {
            var layout = CreateRowLayout();
            layout.Controls.Add(include, 0, 0);
            value.Dock = DockStyle.Fill;
            layout.Controls.Add(value, 1, 0);
            Controls.Add(layout);
        }
    }

    private sealed class OptionalBooleanRow : UserControl
    {
        private readonly CheckBox _include = new();
        private readonly ComboBox _value = CreateChoiceCombo("Disabled", "Enabled");

        public OptionalBooleanRow(string label)
        {
            AutoSize = true;
            Dock = DockStyle.Top;
            _include.Text = label;
            _include.AutoSize = true;
            var layout = CreateRowLayout();
            layout.Controls.Add(_include, 0, 0);
            layout.Controls.Add(_value, 1, 0);
            Controls.Add(layout);
            _include.CheckedChanged += (_, _) => _value.Enabled = _include.Checked;
            _value.Enabled = false;
        }

        public void SetValue(bool? value)
        {
            _include.Checked = value.HasValue;
            _value.SelectedIndex = value == true ? 1 : 0;
        }

        public bool? GetValue()
        {
            return _include.Checked ? _value.SelectedIndex == 1 : null;
        }
    }

    private sealed class OptionalEnumRow<T> : UserControl where T : struct, Enum
    {
        private readonly CheckBox _include = new();
        private readonly ComboBox _value = new();

        public OptionalEnumRow(
            string label,
            Func<T, string>? formatter = null)
        {
            AutoSize = true;
            Dock = DockStyle.Top;
            _include.Text = label;
            _include.AutoSize = true;
            _value.DropDownStyle = ComboBoxStyle.DropDownList;
            _value.Items.AddRange(Enum.GetValues<T>().Cast<object>().ToArray());
            _value.SelectedIndex = 0;
            if (formatter is not null)
            {
                _value.FormattingEnabled = true;
                _value.Format += (_, eventArgs) =>
                {
                    if (eventArgs.ListItem is T item)
                    {
                        eventArgs.Value = formatter(item);
                    }
                };
            }
            var layout = CreateRowLayout();
            layout.Controls.Add(_include, 0, 0);
            layout.Controls.Add(_value, 1, 0);
            Controls.Add(layout);
            _include.CheckedChanged += (_, _) => _value.Enabled = _include.Checked;
            _value.Enabled = false;
        }

        public void SetValue(T? value)
        {
            _include.Checked = value.HasValue;
            if (!value.HasValue)
            {
                _value.SelectedIndex = 0;
                return;
            }

            var index = _value.Items.Cast<object>().ToList().FindIndex(
                item => item.Equals(value.Value));
            _value.SelectedIndex = index >= 0 ? index : 0;
        }

        public T? GetValue()
        {
            return _include.Checked ? (T)_value.SelectedItem! : null;
        }
    }

    private sealed class HudSettingsEditor : UserControl
    {
        private readonly CheckBox _include = new()
        {
            Text = "Visible HUD",
            AutoSize = true
        };
        private readonly ComboBox _visibleHud = CreateEnumCombo<VisibleHudMode>();
        private readonly ComboBox _performanceHud =
            CreateEnumCombo<PerformanceHudMode>();
        private readonly ComboBox _stereoDebugHud =
            CreateEnumCombo<StereoDebugHudMode>();
        private readonly ComboBox _layerHud = CreateEnumCombo<LayerHudMode>();
        private readonly TableLayoutPanel _performanceRow;
        private readonly TableLayoutPanel _stereoRow;
        private readonly TableLayoutPanel _layerRow;

        public HudSettingsEditor()
        {
            AutoSize = true;
            Dock = DockStyle.Top;
            _performanceHud.SelectedItem = PerformanceHudMode.PerformanceSummary;
            _stereoDebugHud.SelectedItem = StereoDebugHudMode.Quad;
            _layerHud.SelectedItem = LayerHudMode.LayerInfo;

            var layout = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            layout.SizeChanged += (_, _) =>
            {
                foreach (Control control in layout.Controls)
                {
                    control.Width = Math.Max(300, layout.ClientSize.Width);
                }
            };
            layout.Controls.Add(CreateHudRow(_include, _visibleHud));
            _performanceRow = CreateHudRow("Performance mode", _performanceHud);
            _stereoRow = CreateHudRow("Stereo debug mode", _stereoDebugHud);
            _layerRow = CreateHudRow("Layer mode", _layerHud);
            layout.Controls.Add(_performanceRow);
            layout.Controls.Add(_stereoRow);
            layout.Controls.Add(_layerRow);
            Controls.Add(layout);

            _include.CheckedChanged += (_, _) => UpdateState();
            _visibleHud.SelectedIndexChanged += (_, _) => UpdateState();
            UpdateState();
        }

        public void LoadSettings(OculusSettings settings)
        {
            var visibleHud = settings.VisibleHud ?? InferLegacyVisibleHud(settings);
            _include.Checked = visibleHud.HasValue;
            SelectEnumValue(_visibleHud, visibleHud ?? VisibleHudMode.None);
            SelectEnumValue(
                _performanceHud,
                settings.PerformanceHud is null or PerformanceHudMode.None
                    ? PerformanceHudMode.PerformanceSummary
                    : settings.PerformanceHud.Value);
            SelectEnumValue(
                _stereoDebugHud,
                settings.StereoDebugHud is null or StereoDebugHudMode.None
                    ? StereoDebugHudMode.Quad
                    : settings.StereoDebugHud.Value);
            SelectEnumValue(
                _layerHud,
                settings.LayerHud is null or LayerHudMode.None
                    ? LayerHudMode.LayerInfo
                    : settings.LayerHud.Value);
            UpdateState();
        }

        public void WriteSettings(OculusSettings settings)
        {
            settings.VisibleHud = _include.Checked
                ? (VisibleHudMode)_visibleHud.SelectedItem!
                : null;
            settings.PerformanceHud = settings.VisibleHud == VisibleHudMode.Performance
                ? (PerformanceHudMode)_performanceHud.SelectedItem!
                : null;
            settings.StereoDebugHud = settings.VisibleHud == VisibleHudMode.StereoDebug
                ? (StereoDebugHudMode)_stereoDebugHud.SelectedItem!
                : null;
            settings.LayerHud = settings.VisibleHud == VisibleHudMode.Layer
                ? (LayerHudMode)_layerHud.SelectedItem!
                : null;
        }

        private void UpdateState()
        {
            _visibleHud.Enabled = _include.Checked;
            var mode = _include.Checked
                ? (VisibleHudMode)_visibleHud.SelectedItem!
                : VisibleHudMode.None;
            _performanceRow.Visible = mode == VisibleHudMode.Performance;
            _stereoRow.Visible = mode == VisibleHudMode.StereoDebug;
            _layerRow.Visible = mode == VisibleHudMode.Layer;
        }

        private static VisibleHudMode? InferLegacyVisibleHud(
            OculusSettings settings)
        {
            if (settings.PerformanceHud is not null and not PerformanceHudMode.None)
            {
                return VisibleHudMode.Performance;
            }

            if (settings.StereoDebugHud is not null and not StereoDebugHudMode.None)
            {
                return VisibleHudMode.StereoDebug;
            }

            if (settings.LayerHud is not null and not LayerHudMode.None)
            {
                return VisibleHudMode.Layer;
            }

            return null;
        }

        private static TableLayoutPanel CreateHudRow(
            CheckBox include,
            Control value)
        {
            var layout = CreateHudRowLayout();
            layout.Controls.Add(include, 0, 0);
            value.Dock = DockStyle.Fill;
            layout.Controls.Add(value, 1, 0);
            return layout;
        }

        private static TableLayoutPanel CreateHudRow(
            string label,
            Control value)
        {
            var layout = CreateHudRowLayout();
            layout.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);
            value.Dock = DockStyle.Fill;
            layout.Controls.Add(value, 1, 0);
            return layout;
        }

        private static ComboBox CreateEnumCombo<T>() where T : struct, Enum
        {
            var comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                FormattingEnabled = true
            };
            comboBox.Items.AddRange(Enum.GetValues<T>().Cast<object>().ToArray());
            comboBox.SelectedIndex = 0;
            comboBox.Format += (_, eventArgs) =>
            {
                eventArgs.Value = FormatEnumName(
                    eventArgs.ListItem?.ToString() ?? string.Empty);
            };
            return comboBox;
        }

        private static void SelectEnumValue<T>(ComboBox comboBox, T value)
            where T : struct, Enum
        {
            var index = comboBox.Items
                .Cast<object>()
                .ToList()
                .FindIndex(item => item.Equals(value));
            comboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        private static TableLayoutPanel CreateHudRowLayout()
        {
            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                ColumnCount = 2,
                Padding = new Padding(0, 2, 0, 2)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            return layout;
        }

        private static string FormatEnumName(string value)
        {
            return Regex.Replace(value, "(?<=[a-z0-9])(?=[A-Z])", " ");
        }
    }

    private static TableLayoutPanel CreateRowLayout()
    {
        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Padding = new Padding(0, 2, 0, 2)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        return layout;
    }

    private static ComboBox CreateChoiceCombo(params string[] values)
    {
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        comboBox.Items.AddRange(values);
        comboBox.SelectedIndex = 0;
        return comboBox;
    }
}
