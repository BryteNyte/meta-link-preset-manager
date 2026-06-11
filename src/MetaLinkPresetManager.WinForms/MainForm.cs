using System.Diagnostics;
using MetaLinkPresetManager.Core.Models;
using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.WinForms;

public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly PresetStore _store;
    private readonly IAppSettingsRepository _settingsRepository;
    private readonly IPresetRepository _presetRepository;
    private readonly IOculusDebugToolService _debugToolService;
    private readonly IGameLauncher _gameLauncher;
    private readonly IProcessWatcherService _processWatcher;
    private readonly IAppLogger _logger;
    private readonly ListBox _presetList = new();
    private readonly TextBox _name = new();
    private readonly CheckBox _enabled = new();
    private readonly TextBox _processName = new();
    private readonly ComboBox _launchType = new();
    private readonly TextBox _launchTarget = new();
    private readonly CheckBox _autoApply = new();
    private readonly CheckBox _restoreOnExit = new();
    private readonly CheckBox _fovIncluded = new();
    private readonly NumericUpDown _fovHorizontal = CreateDecimalInput(0, 2, 0.01m);
    private readonly NumericUpDown _fovVertical = CreateDecimalInput(0, 2, 0.01m);
    private readonly CheckBox _aswIncluded = new();
    private readonly ComboBox _aswMode = CreateEnumCombo<AswMode>();
    private readonly CheckBox _bitrateIncluded = new();
    private readonly NumericUpDown _bitrate = CreateIntegerInput(0, 2000);
    private readonly CheckBox _resolutionIncluded = new();
    private readonly NumericUpDown _resolution = CreateIntegerInput(0, 10000);
    private readonly CheckBox _sharpeningIncluded = new();
    private readonly ComboBox _sharpening = CreateEnumCombo<LinkSharpeningMode>();
    private readonly CheckBox _dimmingIncluded = new();
    private readonly ComboBox _dimming = CreateEnumCombo<LocalDimmingMode>();
    private readonly Label _status = new();
    private readonly TextBox _log = new();
    private readonly List<Button> _actionButtons = [];
    private string? _lastGeneratedCommandFile;
    private bool _closingAfterRestore;

    public MainForm(
        AppSettings settings,
        PresetStore store,
        IAppSettingsRepository settingsRepository,
        IPresetRepository presetRepository,
        IOculusDebugToolService debugToolService,
        IGameLauncher gameLauncher,
        IProcessWatcherService processWatcher,
        IAppLogger logger)
    {
        _settings = settings;
        _store = store;
        _settingsRepository = settingsRepository;
        _presetRepository = presetRepository;
        _debugToolService = debugToolService;
        _gameLauncher = gameLauncher;
        _processWatcher = processWatcher;
        _logger = logger;

        Text = "Meta Link Preset Manager";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 760);
        Size = new Size(1240, 860);

        BuildLayout();
        WireEvents();
        RefreshPresetList();
        _processWatcher.Start();
        UpdateStatus("Ready. Process watcher is running.");
    }

    private OculusPreset? SelectedPreset => _presetList.SelectedItem as OculusPreset;

    private void BuildLayout()
    {
        var root = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 280,
            Panel1MinSize = 240
        };
        root.Panel1.Controls.Add(BuildPresetListPanel());
        root.Panel2.Controls.Add(BuildEditorPanel());
        Controls.Add(root);
    }

    private Control BuildPresetListPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 3,
            ColumnCount = 1
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label
        {
            Text = "Presets",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true
        }, 0, 0);
        _presetList.Dock = DockStyle.Fill;
        _presetList.IntegralHeight = false;
        panel.Controls.Add(_presetList, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };
        buttons.Controls.Add(CreateButton("Add", AddPreset));
        buttons.Controls.Add(CreateButton("Duplicate", DuplicatePreset));
        buttons.Controls.Add(CreateButton("Delete", DeletePreset));
        buttons.Controls.Add(CreateButton("Settings", OpenSettings));
        panel.Controls.Add(buttons, 0, 2);
        return panel;
    }

    private Control BuildEditorPanel()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            RowCount = 4,
            ColumnCount = 1
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));

        outer.Controls.Add(BuildGeneralGroup(), 0, 0);
        outer.Controls.Add(BuildSettingsGroup(), 0, 1);
        outer.Controls.Add(BuildActionPanel(), 0, 2);
        outer.Controls.Add(BuildStatusPanel(), 0, 3);
        return outer;
    }

    private Control BuildGeneralGroup()
    {
        var group = new GroupBox
        {
            Text = "Preset",
            Dock = DockStyle.Top,
            AutoSize = true
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        AddLabeledControl(layout, "Name", _name, 0);
        _enabled.Text = "Preset enabled";
        _enabled.AutoSize = true;
        layout.Controls.Add(_enabled, 1, 1);
        AddLabeledControl(layout, "Process name", _processName, 2);

        _launchType.Items.AddRange(
            Enum.GetValues<LaunchType>().Cast<object>().ToArray());
        _launchType.SelectedIndex = 0;
        _launchType.DropDownStyle = ComboBoxStyle.DropDownList;
        AddLabeledControl(layout, "Launch type", _launchType, 3);
        AddLabeledControl(layout, "Launch target", _launchTarget, 4);
        var browseButton = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        browseButton.Click += BrowseLaunchTarget;
        layout.Controls.Add(browseButton, 2, 4);

        var options = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        _autoApply.Text = "Apply automatically when process starts";
        _restoreOnExit.Text = "Restore default when process exits";
        _autoApply.AutoSize = true;
        _restoreOnExit.AutoSize = true;
        options.Controls.Add(_autoApply);
        options.Controls.Add(_restoreOnExit);
        layout.Controls.Add(options, 1, 5);
        layout.SetColumnSpan(options, 2);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildSettingsGroup()
    {
        var group = new GroupBox
        {
            Text = "Oculus Link Settings",
            Dock = DockStyle.Top,
            AutoSize = true
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        _fovIncluded.Text = "FOV tangent multiplier";
        layout.Controls.Add(_fovIncluded, 0, 0);
        layout.Controls.Add(_fovHorizontal, 1, 0);
        layout.Controls.Add(new Label
        {
            Text = "Vertical",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 2, 0);
        layout.Controls.Add(_fovVertical, 3, 0);

        AddOptionalSetting(layout, _aswIncluded, "ASW mode", _aswMode, 1);
        AddOptionalSetting(layout, _bitrateIncluded, "Encode bitrate Mbps", _bitrate, 2);
        AddOptionalSetting(layout, _resolutionIncluded, "Encode resolution width", _resolution, 3);
        AddOptionalSetting(layout, _sharpeningIncluded, "Link sharpening", _sharpening, 4);
        AddOptionalSetting(layout, _dimmingIncluded, "Local dimming", _dimming, 5);

        group.Controls.Add(layout);
        return group;
    }

    private Control BuildActionPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 8),
            WrapContents = true
        };
        panel.Controls.Add(CreateActionButton("Save Preset", SavePreset));
        panel.Controls.Add(CreateActionButton("Apply Preset", ApplySelectedPreset));
        panel.Controls.Add(CreateActionButton("Apply + Launch", ApplyAndLaunch));
        panel.Controls.Add(CreateActionButton("Launch Only", LaunchSelectedPreset));
        panel.Controls.Add(CreateActionButton("Restore Default", RestoreDefaultPreset));
        panel.Controls.Add(CreateActionButton("Open Command File", OpenGeneratedCommandFile));
        panel.Controls.Add(CreateActionButton("Test CLI Path", TestCliPath));
        return panel;
    }

    private Control BuildStatusPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _status.Text = "Starting...";
        _status.AutoSize = true;
        _status.Padding = new Padding(0, 0, 0, 6);
        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Font = new Font(FontFamily.GenericMonospace, 9);
        panel.Controls.Add(_status, 0, 0);
        panel.Controls.Add(_log, 0, 1);
        return panel;
    }

    private void WireEvents()
    {
        _presetList.SelectedIndexChanged += (_, _) => LoadSelectedPreset();
        _launchType.SelectedIndexChanged += (_, _) => UpdateLaunchTargetState();
        _fovIncluded.CheckedChanged += (_, _) => UpdateOptionalControlStates();
        _aswIncluded.CheckedChanged += (_, _) => UpdateOptionalControlStates();
        _bitrateIncluded.CheckedChanged += (_, _) => UpdateOptionalControlStates();
        _resolutionIncluded.CheckedChanged += (_, _) => UpdateOptionalControlStates();
        _sharpeningIncluded.CheckedChanged += (_, _) => UpdateOptionalControlStates();
        _dimmingIncluded.CheckedChanged += (_, _) => UpdateOptionalControlStates();
        _logger.LogWritten += LoggerOnLogWritten;
        _processWatcher.ProcessStarted += ProcessWatcherOnProcessStarted;
        _processWatcher.ProcessExited += ProcessWatcherOnProcessExited;
        FormClosing += MainFormOnFormClosing;
        FormClosed += (_, _) =>
        {
            _logger.LogWritten -= LoggerOnLogWritten;
            _processWatcher.Dispose();
            _logger.Info("Application exited.");
        };
    }

    private void RefreshPresetList(string? selectedId = null)
    {
        selectedId ??= SelectedPreset?.Id;
        _presetList.BeginUpdate();
        _presetList.Items.Clear();
        foreach (var preset in _store.Presets.OrderBy(preset => preset.Name))
        {
            _presetList.Items.Add(preset);
        }
        _presetList.EndUpdate();

        var selected = _store.Presets.FirstOrDefault(preset =>
            string.Equals(preset.Id, selectedId, StringComparison.OrdinalIgnoreCase));
        _presetList.SelectedItem = selected ?? _presetList.Items.Cast<object>().FirstOrDefault();
    }

    private void LoadSelectedPreset()
    {
        var preset = SelectedPreset;
        SetEditorEnabled(preset is not null);
        if (preset is null)
        {
            return;
        }

        _name.Text = preset.Name;
        _enabled.Checked = preset.Enabled;
        _processName.Text = preset.ProcessName ?? string.Empty;
        SelectEnumValue(_launchType, preset.LaunchType);
        _launchTarget.Text = preset.LaunchTarget ?? string.Empty;
        _autoApply.Checked = preset.ApplyAutomaticallyWhenProcessStarts;
        _restoreOnExit.Checked = preset.RestoreDefaultPresetOnExit;

        SetNullableValue(
            _fovIncluded,
            preset.Settings.FovTanMultiplierHorizontal,
            _fovHorizontal);
        if (preset.Settings.FovTanMultiplierVertical.HasValue)
        {
            _fovVertical.Value = Clamp(
                preset.Settings.FovTanMultiplierVertical.Value,
                _fovVertical);
        }
        _aswIncluded.Checked = preset.Settings.AswMode.HasValue;
        SelectEnumValue(_aswMode, preset.Settings.AswMode ?? AswMode.Auto);
        SetNullableValue(_bitrateIncluded, preset.Settings.EncodeBitrateMbps, _bitrate);
        SetNullableValue(
            _resolutionIncluded,
            preset.Settings.EncodeResolutionWidth,
            _resolution);
        _sharpeningIncluded.Checked = preset.Settings.LinkSharpening.HasValue;
        SelectEnumValue(
            _sharpening,
            preset.Settings.LinkSharpening ?? LinkSharpeningMode.Default);
        _dimmingIncluded.Checked = preset.Settings.LocalDimming.HasValue;
        SelectEnumValue(
            _dimming,
            preset.Settings.LocalDimming ?? LocalDimmingMode.Default);
        UpdateOptionalControlStates();
        UpdateLaunchTargetState();
    }

    private OculusPreset ReadEditor(OculusPreset source)
    {
        var preset = source.Clone();
        preset.Name = _name.Text.Trim();
        preset.Enabled = _enabled.Checked;
        preset.ProcessName = NullIfWhiteSpace(_processName.Text);
        preset.LaunchType = (LaunchType)_launchType.SelectedItem!;
        preset.LaunchTarget = NullIfWhiteSpace(_launchTarget.Text);
        preset.ApplyAutomaticallyWhenProcessStarts = _autoApply.Checked;
        preset.RestoreDefaultPresetOnExit = _restoreOnExit.Checked;
        preset.Settings.FovTanMultiplierHorizontal =
            _fovIncluded.Checked ? _fovHorizontal.Value : null;
        preset.Settings.FovTanMultiplierVertical =
            _fovIncluded.Checked ? _fovVertical.Value : null;
        preset.Settings.AswMode =
            _aswIncluded.Checked ? (AswMode)_aswMode.SelectedItem! : null;
        preset.Settings.EncodeBitrateMbps =
            _bitrateIncluded.Checked ? (int)_bitrate.Value : null;
        preset.Settings.EncodeResolutionWidth =
            _resolutionIncluded.Checked ? (int)_resolution.Value : null;
        preset.Settings.LinkSharpening = _sharpeningIncluded.Checked
            ? (LinkSharpeningMode)_sharpening.SelectedItem!
            : null;
        preset.Settings.LocalDimming = _dimmingIncluded.Checked
            ? (LocalDimmingMode)_dimming.SelectedItem!
            : null;
        return preset;
    }

    private async void SavePreset(object? sender, EventArgs e)
    {
        await SaveSelectedPresetAsync();
    }

    private void AddPreset(object? sender, EventArgs e)
    {
        var preset = new OculusPreset
        {
            Name = "New Preset",
            ApplyAutomaticallyWhenProcessStarts = false,
            RestoreDefaultPresetOnExit = false
        };
        _store.Presets.Add(preset);
        RefreshPresetList(preset.Id);
        _name.SelectAll();
        _name.Focus();
    }

    private async void DuplicatePreset(object? sender, EventArgs e)
    {
        var selected = SelectedPreset;
        if (selected is null)
        {
            return;
        }

        var duplicate = selected.Clone(true);
        duplicate.Name = $"{selected.Name} Copy";
        _store.Presets.Add(duplicate);
        await _presetRepository.SaveAsync(_store);
        _logger.Info($"Preset duplicated: {selected.Name}");
        RefreshPresetList(duplicate.Id);
    }

    private async void DeletePreset(object? sender, EventArgs e)
    {
        var selected = SelectedPreset;
        if (selected is null)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete preset \"{selected.Name}\"?",
            "Delete Preset",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _store.Presets.Remove(selected);
        if (string.Equals(
                _settings.DefaultPresetId,
                selected.Id,
                StringComparison.OrdinalIgnoreCase))
        {
            _settings.DefaultPresetId = null;
            await _settingsRepository.SaveAsync(_settings);
        }
        await _presetRepository.SaveAsync(_store);
        _logger.Info($"Preset deleted: {selected.Name}");
        RefreshPresetList();
    }

    private async void OpenSettings(object? sender, EventArgs e)
    {
        using var dialog = new SettingsForm(_settings, _store.Presets);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        CopySettings(dialog.Settings, _settings);
        await _settingsRepository.SaveAsync(_settings);
        _processWatcher.UpdateInterval(
            TimeSpan.FromSeconds(_settings.ProcessPollingIntervalSeconds));
        _logger.Info("Application settings saved.");
        UpdateStatus("Application settings saved.");
    }

    private async void ApplySelectedPreset(object? sender, EventArgs e)
    {
        var preset = await SaveSelectedPresetAsync();
        if (preset is not null)
        {
            await ApplyPresetAsync(preset);
        }
    }

    private async void ApplyAndLaunch(object? sender, EventArgs e)
    {
        var preset = await SaveSelectedPresetAsync();
        if (preset is null)
        {
            return;
        }

        var applyResult = await ApplyPresetAsync(preset);
        if (applyResult)
        {
            await LaunchAsync(preset);
        }
    }

    private async void LaunchSelectedPreset(object? sender, EventArgs e)
    {
        var preset = SelectedPreset;
        if (preset is not null)
        {
            await LaunchAsync(preset);
        }
    }

    private async void RestoreDefaultPreset(object? sender, EventArgs e)
    {
        await RestoreDefaultAsync();
    }

    private async void OpenGeneratedCommandFile(object? sender, EventArgs e)
    {
        var preset = SelectedPreset;
        if (preset is null)
        {
            return;
        }

        try
        {
            _lastGeneratedCommandFile =
                await _debugToolService.GenerateCommandFileAsync(preset);
            Process.Start(new ProcessStartInfo
            {
                FileName = _lastGeneratedCommandFile,
                UseShellExecute = true
            });
            UpdateStatus($"Opened command file: {_lastGeneratedCommandFile}");
        }
        catch (Exception exception)
        {
            _logger.Error("Could not open generated command file", exception);
            ShowError(exception.Message);
        }
    }

    private void TestCliPath(object? sender, EventArgs e)
    {
        var exists = _debugToolService.CliExists();
        UpdateStatus(exists
            ? $"CLI found: {_settings.OculusDebugToolCliPath}"
            : $"CLI missing: {_settings.OculusDebugToolCliPath}");
        MessageBox.Show(
            this,
            _status.Text,
            "CLI Path",
            MessageBoxButtons.OK,
            exists ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private async Task<bool> ApplyPresetAsync(OculusPreset preset)
    {
        SetBusy(true);
        UpdateStatus($"Applying preset: {preset.Name}");
        try
        {
            var result = await _debugToolService.ApplyPresetAsync(preset);
            _lastGeneratedCommandFile = result.CommandFilePath;

            if (result.Succeeded)
            {
                UpdateStatus($"Applied preset: {preset.Name}");
                return true;
            }

            UpdateStatus($"Apply failed: {result.ErrorMessage}");
            ShowError(result.ErrorMessage ?? "The preset could not be applied.");
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task<OculusPreset?> SaveSelectedPresetAsync()
    {
        var selected = SelectedPreset;
        if (selected is null)
        {
            return null;
        }

        var candidate = ReadEditor(selected);
        var errors = PresetValidator.ValidateForSave(
            candidate,
            _store.Presets.Where(preset => !ReferenceEquals(preset, selected)));
        if (errors.Count > 0)
        {
            ShowErrors("Preset could not be saved", errors);
            return null;
        }

        var index = _store.Presets.IndexOf(selected);
        _store.Presets[index] = candidate;
        await _presetRepository.SaveAsync(_store);
        _logger.Info($"Preset saved: {candidate.Name}");
        RefreshPresetList(candidate.Id);
        UpdateStatus($"Saved preset: {candidate.Name}");
        return candidate;
    }

    private async Task LaunchAsync(OculusPreset preset)
    {
        var result = await _gameLauncher.LaunchAsync(preset);
        if (result.Succeeded)
        {
            UpdateStatus($"Launch target started: {preset.Name}");
            return;
        }

        UpdateStatus($"Launch failed: {result.ErrorMessage}");
        ShowError(result.ErrorMessage ?? "The launch target could not be started.");
    }

    private async Task<bool> RestoreDefaultAsync()
    {
        var preset = _store.Presets.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Id,
                _settings.DefaultPresetId,
                StringComparison.OrdinalIgnoreCase));
        if (preset is null)
        {
            ShowError("No valid default preset is configured.");
            return false;
        }

        _logger.Info($"Restoring default preset: {preset.Name}");
        return await ApplyPresetAsync(preset);
    }

    private void ProcessWatcherOnProcessStarted(
        object? sender,
        WatchedProcessEventArgs e)
    {
        RunOnUiThread(async () =>
        {
            UpdateStatus($"Detected {e.Preset.ProcessName}; applying {e.Preset.Name}.");
            await ApplyPresetAsync(e.Preset);
        });
    }

    private void ProcessWatcherOnProcessExited(
        object? sender,
        WatchedProcessEventArgs e)
    {
        RunOnUiThread(async () =>
        {
            UpdateStatus($"Detected exit: {e.Preset.ProcessName}");
            if (e.Preset.RestoreDefaultPresetOnExit)
            {
                await RestoreDefaultAsync();
            }
        });
    }

    private async void MainFormOnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_closingAfterRestore)
        {
            return;
        }

        _processWatcher.Stop();
        if (!_settings.RestoreDefaultPresetOnAppExit)
        {
            return;
        }

        e.Cancel = true;
        _closingAfterRestore = true;
        await RestoreDefaultAsync();
        Close();
    }

    private void LoggerOnLogWritten(object? sender, string line)
    {
        if (IsDisposed)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => LoggerOnLogWritten(sender, line));
            return;
        }

        _log.AppendText(line + Environment.NewLine);
    }

    private void BrowseLaunchTarget(object? sender, EventArgs e)
    {
        var launchType = (LaunchType)_launchType.SelectedItem!;
        if (launchType is not (LaunchType.Exe or LaunchType.Shortcut))
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = launchType == LaunchType.Exe
                ? "Executable files|*.exe|All files|*.*"
                : "Windows shortcuts|*.lnk|All files|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _launchTarget.Text = dialog.FileName;
        }
    }

    private void UpdateOptionalControlStates()
    {
        _fovHorizontal.Enabled = _fovIncluded.Checked;
        _fovVertical.Enabled = _fovIncluded.Checked;
        _aswMode.Enabled = _aswIncluded.Checked;
        _bitrate.Enabled = _bitrateIncluded.Checked;
        _resolution.Enabled = _resolutionIncluded.Checked;
        _sharpening.Enabled = _sharpeningIncluded.Checked;
        _dimming.Enabled = _dimmingIncluded.Checked;
    }

    private void UpdateLaunchTargetState()
    {
        if (_launchType.SelectedItem is not LaunchType launchType)
        {
            return;
        }

        _launchTarget.Enabled = launchType != LaunchType.None;
    }

    private void SetEditorEnabled(bool enabled)
    {
        foreach (Control control in new Control[]
                 {
                     _name, _enabled, _processName, _launchType, _launchTarget,
                     _autoApply, _restoreOnExit, _fovIncluded, _aswIncluded,
                     _bitrateIncluded, _resolutionIncluded, _sharpeningIncluded,
                     _dimmingIncluded
                 })
        {
            control.Enabled = enabled;
        }
        UpdateOptionalControlStates();
    }

    private void SetBusy(bool busy)
    {
        foreach (var button in _actionButtons)
        {
            button.Enabled = !busy;
        }
        UseWaitCursor = busy;
    }

    private void UpdateStatus(string message)
    {
        _status.Text = message;
    }

    private void RunOnUiThread(Func<Task> action)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }
        BeginInvoke(async () => await action());
    }

    private static void CopySettings(AppSettings source, AppSettings target)
    {
        target.OculusDebugToolCliPath = source.OculusDebugToolCliPath;
        target.RequireElevationForCli = source.RequireElevationForCli;
        target.ProcessPollingIntervalSeconds = source.ProcessPollingIntervalSeconds;
        target.RestoreDefaultPresetOnAppExit = source.RestoreDefaultPresetOnAppExit;
        target.DefaultPresetId = source.DefaultPresetId;
    }

    private static Button CreateButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += handler;
        return button;
    }

    private Button CreateActionButton(string text, EventHandler handler)
    {
        var button = CreateButton(text, handler);
        _actionButtons.Add(button);
        return button;
    }

    private static void AddLabeledControl(
        TableLayoutPanel layout,
        string label,
        Control control,
        int row)
    {
        layout.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
    }

    private static void AddOptionalSetting(
        TableLayoutPanel layout,
        CheckBox include,
        string label,
        Control control,
        int row)
    {
        include.Text = label;
        include.AutoSize = true;
        layout.Controls.Add(include, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
        layout.SetColumnSpan(control, 3);
    }

    private static NumericUpDown CreateDecimalInput(
        decimal minimum,
        int decimalPlaces,
        decimal increment)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = 10,
            DecimalPlaces = decimalPlaces,
            Increment = increment,
            Dock = DockStyle.Fill
        };
    }

    private static NumericUpDown CreateIntegerInput(decimal minimum, decimal maximum)
    {
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            DecimalPlaces = 0,
            Increment = 1,
            Dock = DockStyle.Fill,
            ThousandsSeparator = true
        };
    }

    private static ComboBox CreateEnumCombo<T>() where T : struct, Enum
    {
        var comboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Dock = DockStyle.Fill
        };
        comboBox.Items.AddRange(Enum.GetValues<T>().Cast<object>().ToArray());
        comboBox.SelectedIndex = 0;
        return comboBox;
    }

    private static void SelectEnumValue<T>(ComboBox comboBox, T value)
        where T : struct, Enum
    {
        var index = comboBox.Items
            .Cast<object>()
            .Select((item, itemIndex) => new { item, itemIndex })
            .FirstOrDefault(entry => entry.item.Equals(value))
            ?.itemIndex;
        comboBox.SelectedIndex = index ?? 0;
    }

    private static void SetNullableValue(
        CheckBox include,
        decimal? value,
        NumericUpDown control)
    {
        include.Checked = value.HasValue;
        if (value.HasValue)
        {
            control.Value = Clamp(value.Value, control);
        }
    }

    private static void SetNullableValue(
        CheckBox include,
        int? value,
        NumericUpDown control)
    {
        SetNullableValue(
            include,
            value.HasValue ? (decimal?)value.Value : null,
            control);
    }

    private static decimal Clamp(decimal value, NumericUpDown control)
    {
        return Math.Clamp(value, control.Minimum, control.Maximum);
    }

    private static string? NullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void ShowError(string message)
    {
        MessageBox.Show(
            this,
            message,
            "Meta Link Preset Manager",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void ShowErrors(string title, IReadOnlyList<string> errors)
    {
        MessageBox.Show(
            this,
            string.Join(Environment.NewLine, errors.Select(error => $"- {error}")),
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
