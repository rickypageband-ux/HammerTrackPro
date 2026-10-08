using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

/// <summary>Shows licensing status on either workstation and locks all open dialogs at expiry.</summary>
internal sealed class LicenseMonitor : IDisposable
{
    private readonly Form _root;
    private readonly LicenseViewModel _viewModel = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly ToolStripStatusLabel _status = new();
    private bool _prompting;
    public LicenseMonitor(Form root)
    {
        _root = root;
        var strip = new StatusStrip { Dock = DockStyle.Bottom, AutoSize = false, Height = 30 };
        strip.Items.Add(_status);
        var activate = new ToolStripButton("Import / Upgrade License");
        activate.Click += (_, _) => ShowActivation();
        strip.Items.Add(activate);
        root.Controls.Add(strip);
        strip.SendToBack();
        _status.Text = _viewModel.Status;
        _timer.Tick += (_, _) => Check();
        _timer.Start();
    }
    private void Check()
    {
        if (_prompting || _root.IsDisposed) return;
        _status.Text = _viewModel.Status;
        if (!_viewModel.IsActive) ShowActivation();
    }
    private void ShowActivation()
    {
        if (_prompting) return;
        _prompting = true;
        try
        {
            using var dialog = new LicenseActivationForm(_viewModel.IsActive ? _viewModel.Status : "Your license has expired. Import a paid license to continue. Your auction data is preserved.");
            dialog.ShowDialog(Form.ActiveForm ?? _root);
            if (!_viewModel.IsActive) Application.Exit();
            else _status.Text = _viewModel.Status;
        }
        finally { _prompting = false; }
    }
    internal static bool AllowFeature(IWin32Window owner, string feature)
    {
        if (new LicenseViewModel().CanUseFeature(feature, out var reason)) return true;
        MessageBox.Show(owner, reason, "Demo limitation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }
    public void Dispose() => _timer.Dispose();
}
