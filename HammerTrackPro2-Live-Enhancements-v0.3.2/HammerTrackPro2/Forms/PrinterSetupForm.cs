using System.Drawing.Printing;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class PrinterSetupForm : Form
{
    private readonly PrinterSetupViewModel _viewModel;
    private readonly ComboBox _printers = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.DimGray };

    public PrinterSetupForm(PrinterSetupViewModel? viewModel = null)
    {
        _viewModel = viewModel ?? new PrinterSetupViewModel(new WindowsPrinterCatalog(), new WorkstationPrinterPreferenceStore());
        Text = "HammerTrack Pro — Printer Setup";
        StartPosition = FormStartPosition.CenterParent;
        Width = 650;
        Height = 330;
        MinimumSize = new Size(560, 300);
        Font = new Font("Segoe UI", 10F);

        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), RowCount = 6, ColumnCount = 1 };
        panel.Controls.Add(new Label { Text = "PRINTER SETUP", AutoSize = true, Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 50, 74) });
        panel.Controls.Add(new Label { Text = "Choose the printer this computer will use for HammerTrack documents. The choice is saved on this PC and can be changed here at any time.", AutoSize = true, MaximumSize = new Size(570, 0), Margin = new Padding(0, 10, 0, 14) });
        panel.Controls.Add(new Label { Text = "Printer", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold) });
        _printers.DataSource = _viewModel.Printers.ToList();
        if (!string.IsNullOrWhiteSpace(_viewModel.SelectedPrinter)) _printers.SelectedItem = _viewModel.SelectedPrinter;
        panel.Controls.Add(_printers);
        panel.Controls.Add(_status);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(0, 14, 0, 0) };
        actions.Controls.Add(Button("Save Printer", Save, true));
        actions.Controls.Add(Button("Print Test Page", PrintTestPage));
        actions.Controls.Add(Button("Close", Close));
        panel.Controls.Add(actions);
        Controls.Add(panel);
        _status.Text = _viewModel.Printers.Count == 0 ? "No Windows printers were found." : "Select a printer, then save it for this computer.";
    }

    private static Button Button(string text, Action action, bool accent = false)
    {
        var button = new Button { Text = text, Width = 160, Height = 42, FlatStyle = FlatStyle.Flat, BackColor = accent ? Color.FromArgb(224, 168, 54) : Color.FromArgb(24, 96, 150), ForeColor = accent ? Color.FromArgb(24, 50, 74) : Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        button.Click += (_, _) => action();
        return button;
    }

    private void Save()
    {
        try
        {
            SavePreference();
            _status.Text = $"Saved: {_viewModel.SelectedPrinter}";
            _status.ForeColor = Color.DarkGreen;
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "Printer Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void PrintTestPage()
    {
        try
        {
            SavePreference();
            using var document = new PrintDocument { DocumentName = "HammerTrack Pro Printer Test" };
            PrinterPreferenceService.Apply(document);
            document.PrintPage += (_, e) =>
            {
                var graphics = e.Graphics ?? throw new InvalidOperationException("The printer did not provide a drawing surface.");
                using var heading = new Font("Segoe UI", 22F, FontStyle.Bold);
                using var body = new Font("Segoe UI", 12F);
                graphics.DrawString("HAMMERTRACK PRO", heading, Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Top);
                graphics.DrawString($"Printer test successful\nPrinter: {_viewModel.SelectedPrinter}\nPrinted: {DateTime.Now:g}", body, Brushes.Black, e.MarginBounds.Left, e.MarginBounds.Top + 70);
            };
            document.Print();
            _status.Text = $"Test page sent to {_viewModel.SelectedPrinter}.";
            _status.ForeColor = Color.DarkGreen;
        }
        catch (Exception exception) { MessageBox.Show(this, exception.Message, "Printer Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void SavePreference()
    {
        _viewModel.SelectedPrinter = _printers.SelectedItem?.ToString() ?? string.Empty;
        _viewModel.Save();
    }
}
