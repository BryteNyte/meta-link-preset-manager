using MetaLinkPresetManager.Core.Models;

namespace MetaLinkPresetManager.WinForms;

public sealed class SettingsForm : Form
{
    private readonly TextBox _cliPath = new();
    private readonly CheckBox _requireElevation = new();
    private readonly NumericUpDown _pollingInterval = new();
    private readonly ComboBox _defaultPreset = new();
    private readonly CheckBox _restoreOnExit = new();

    public SettingsForm(AppSettings settings, IReadOnlyList<OculusPreset> presets)
    {
        Settings = settings.Clone();
        Text = "Application Settings";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(680, 330);
        Size = new Size(760, 360);
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildLayout(presets);
        LoadSettings(settings);
    }

    public AppSettings Settings { get; }

    private void BuildLayout(IReadOnlyList<OculusPreset> presets)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 3,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        _cliPath.Dock = DockStyle.Fill;
        var browseButton = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        browseButton.Click += BrowseCliPath;
        layout.Controls.Add(new Label
        {
            Text = "Oculus Debug Tool CLI",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        layout.Controls.Add(_cliPath, 1, 0);
        layout.Controls.Add(browseButton, 2, 0);

        _requireElevation.Text = "Elevate only the CLI process when applying presets";
        _requireElevation.AutoSize = true;
        layout.Controls.Add(_requireElevation, 1, 1);

        _pollingInterval.Minimum = 1;
        _pollingInterval.Maximum = 60;
        _pollingInterval.Width = 100;
        layout.Controls.Add(new Label
        {
            Text = "Polling interval (seconds)",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 2);
        layout.Controls.Add(_pollingInterval, 1, 2);

        _defaultPreset.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultPreset.Dock = DockStyle.Fill;
        _defaultPreset.Items.Add(new PresetChoice(null, "(None)"));
        foreach (var preset in presets.OrderBy(preset => preset.Name))
        {
            _defaultPreset.Items.Add(new PresetChoice(preset.Id, preset.Name));
        }
        layout.Controls.Add(new Label
        {
            Text = "Default preset",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 3);
        layout.Controls.Add(_defaultPreset, 1, 3);

        _restoreOnExit.Text = "Restore the default preset when this app exits";
        _restoreOnExit.AutoSize = true;
        layout.Controls.Add(_restoreOnExit, 1, 4);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        var saveButton = new Button { Text = "Save", AutoSize = true };
        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        var testButton = new Button { Text = "Test CLI Path", AutoSize = true };
        saveButton.Click += SaveSettings;
        testButton.Click += TestCliPath;
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(testButton);
        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 3);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(layout);
    }

    private void LoadSettings(AppSettings settings)
    {
        _cliPath.Text = settings.OculusDebugToolCliPath;
        _requireElevation.Checked = settings.RequireElevationForCli;
        _pollingInterval.Value = Math.Clamp(settings.ProcessPollingIntervalSeconds, 1, 60);
        _restoreOnExit.Checked = settings.RestoreDefaultPresetOnAppExit;

        var match = _defaultPreset.Items
            .Cast<PresetChoice>()
            .FirstOrDefault(choice => string.Equals(
                choice.Id,
                settings.DefaultPresetId,
                StringComparison.OrdinalIgnoreCase));
        _defaultPreset.SelectedItem = match ?? _defaultPreset.Items[0];
    }

    private void BrowseCliPath(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Oculus Debug Tool CLI|OculusDebugToolCLI.exe|Executable files|*.exe",
            FileName = "OculusDebugToolCLI.exe",
            CheckFileExists = true
        };

        if (File.Exists(_cliPath.Text))
        {
            dialog.InitialDirectory = Path.GetDirectoryName(_cliPath.Text);
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _cliPath.Text = dialog.FileName;
        }
    }

    private void TestCliPath(object? sender, EventArgs e)
    {
        var exists = File.Exists(_cliPath.Text.Trim());
        MessageBox.Show(
            this,
            exists
                ? "OculusDebugToolCLI.exe was found."
                : "The selected OculusDebugToolCLI.exe path does not exist.",
            "CLI Path",
            MessageBoxButtons.OK,
            exists ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void SaveSettings(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_cliPath.Text))
        {
            MessageBox.Show(
                this,
                "Select the OculusDebugToolCLI.exe path.",
                "Settings",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Settings.OculusDebugToolCliPath = _cliPath.Text.Trim();
        Settings.RequireElevationForCli = _requireElevation.Checked;
        Settings.ProcessPollingIntervalSeconds = (int)_pollingInterval.Value;
        Settings.RestoreDefaultPresetOnAppExit = _restoreOnExit.Checked;
        Settings.DefaultPresetId = (_defaultPreset.SelectedItem as PresetChoice)?.Id;
        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record PresetChoice(string? Id, string Name)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}
