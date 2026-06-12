using MetaLinkPresetManager.Core.Services;

namespace MetaLinkPresetManager.WinForms;

public sealed class RunningProcessDialog : Form
{
    private readonly RunningProcessListModel _model;
    private readonly ListView _processList = new();
    private readonly Button _selectButton = new();
    private readonly Button _refreshButton = new();

    public RunningProcessDialog(RunningProcessListModel model)
    {
        _model = model;
        Text = "Select Running Process";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(780, 440);
        Size = new Size(980, 560);
        FormBorderStyle = FormBorderStyle.Sizable;

        BuildLayout();
        Shown += async (_, _) => await RefreshProcessesAsync();
    }

    public RunningProcessInfo? SelectedProcess { get; private set; }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label
        {
            Text = "Choose a process to watch. This does not launch or stop the selected application.",
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        }, 0, 0);

        _processList.Dock = DockStyle.Fill;
        _processList.View = View.Details;
        _processList.FullRowSelect = true;
        _processList.MultiSelect = false;
        _processList.HideSelection = false;
        _processList.Columns.Add("Executable", 180);
        _processList.Columns.Add("PID", 80);
        _processList.Columns.Add("Window title", 280);
        _processList.Columns.Add("Path", 400);
        _processList.SelectedIndexChanged += (_, _) =>
        {
            _selectButton.Enabled = _processList.SelectedItems.Count == 1;
        };
        _processList.DoubleClick += (_, _) => SelectProcess();
        layout.Controls.Add(_processList, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        _selectButton.Text = "Select";
        _selectButton.AutoSize = true;
        _selectButton.Enabled = false;
        _selectButton.Click += (_, _) => SelectProcess();
        var cancelButton = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        _refreshButton.Text = "Refresh";
        _refreshButton.AutoSize = true;
        _refreshButton.Click += async (_, _) => await RefreshProcessesAsync();
        buttons.Controls.Add(_selectButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(_refreshButton);
        layout.Controls.Add(buttons, 0, 2);

        AcceptButton = _selectButton;
        CancelButton = cancelButton;
        Controls.Add(layout);
    }

    private async Task RefreshProcessesAsync()
    {
        var beganUpdate = false;
        _refreshButton.Enabled = false;
        _selectButton.Enabled = false;
        UseWaitCursor = true;
        try
        {
            await Task.Run(_model.Refresh);
            if (IsDisposed || Disposing)
            {
                return;
            }

            _processList.BeginUpdate();
            beganUpdate = true;
            _processList.Items.Clear();
            foreach (var process in _model.Processes)
            {
                var item = new ListViewItem(process.ExecutableName)
                {
                    Tag = process
                };
                item.SubItems.Add(
                    process.ProcessId >= 0
                        ? process.ProcessId.ToString()
                        : "(unavailable)");
                item.SubItems.Add(
                    string.IsNullOrWhiteSpace(process.WindowTitle)
                        ? "(none)"
                        : process.WindowTitle);
                item.SubItems.Add(
                    string.IsNullOrWhiteSpace(process.ExecutablePath)
                        ? "(unavailable)"
                        : process.ExecutablePath);
                _processList.Items.Add(item);
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"Running processes could not be refreshed.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "Running Processes",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        finally
        {
            if (beganUpdate)
            {
                _processList.EndUpdate();
            }
            if (!IsDisposed && !Disposing)
            {
                UseWaitCursor = false;
                _refreshButton.Enabled = true;
                _selectButton.Enabled = false;
            }
        }
    }

    private void SelectProcess()
    {
        if (_processList.SelectedItems.Count != 1
            || _processList.SelectedItems[0].Tag is not RunningProcessInfo process)
        {
            return;
        }

        SelectedProcess = process;
        DialogResult = DialogResult.OK;
        Close();
    }
}
