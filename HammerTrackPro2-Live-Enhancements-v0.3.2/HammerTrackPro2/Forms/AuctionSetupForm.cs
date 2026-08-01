using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class AuctionSetupForm : Form
{
    private readonly AuctionSetupViewModel _viewModel;
    private readonly ComboBox _auctions = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Auction.DisplayText), Dock = DockStyle.Fill };
    private readonly PropertyGrid _editor = new() { Dock = DockStyle.Fill, HelpVisible = false, ToolbarVisible = false };
    private readonly CheckBox _copyConsignors = new() { Text = "Copy Consignors", AutoSize = true };

    public AuctionSetupForm(AuctionSetupViewModel viewModel)
    {
        _viewModel = viewModel;
        Text = "Auction Setup";
        Width = 760;
        Height = 720;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.Add(Button("New Auction", () => { _viewModel.NewAuction(); RefreshEditor(); }));
        buttons.Controls.Add(Button("Save", SaveAuction));
        buttons.Controls.Add(Button("Load Auction", LoadAuction));
        buttons.Controls.Add(Button("Close Auction", CloseAuction));
        buttons.Controls.Add(Button("Duplicate Previous Auction", () => { _viewModel.DuplicatePreviousAuction(); RefreshEditor(); }));
        buttons.Controls.Add(_copyConsignors);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(_auctions, 0, 0); layout.Controls.Add(_editor, 0, 1); layout.Controls.Add(buttons, 0, 2);
        Controls.Add(layout);
        Load += (_, _) => { _viewModel.Load(); RefreshEditor(); };
    }

    private static Button Button(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => Execute(action);
        return button;
    }

    private void SaveAuction() { _viewModel.CopyConsignors = _copyConsignors.Checked; _viewModel.Save(); RefreshEditor(); }
    private void LoadAuction()
    {
        if (_auctions.SelectedItem is Auction auction) { _viewModel.LoadAuction(auction.AuctionId); RefreshEditor(); }
    }
    private void CloseAuction()
    {
        if (MessageBox.Show(this, "Close this auction? Operational changes will be locked and reopening is not available in Sprint 2.", "Close Auction", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        _viewModel.CloseAuction(); RefreshEditor();
    }
    private void RefreshEditor()
    {
        _auctions.DataSource = _viewModel.Auctions.ToList();
        _editor.SelectedObject = _viewModel.CurrentAuction;
        _copyConsignors.Checked = _viewModel.CopyConsignors;
        _copyConsignors.Enabled = _viewModel.CurrentAuction.AuctionId == 0;
    }
    private static void Execute(Action action)
    {
        try { action(); }
        catch (Exception exception) { MessageBox.Show(exception.Message, "Auction Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
}
