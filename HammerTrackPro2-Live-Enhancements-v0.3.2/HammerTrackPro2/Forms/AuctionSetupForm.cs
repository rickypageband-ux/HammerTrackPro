using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class AuctionSetupForm : Form
{
    private readonly AuctionSetupViewModel _viewModel;
    private readonly ComboBox _auctions = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Auction.DisplayText), Dock = DockStyle.Fill };
    private readonly PropertyGrid _editor = new() { Dock = DockStyle.Fill, HelpVisible = true, ToolbarVisible = false, PropertySort = PropertySort.Categorized };
    private readonly CheckBox _copyConsignors = new() { Text = "Copy Sellers", AutoSize = true };

    public AuctionSetupForm(AuctionSetupViewModel viewModel)
    {
        _viewModel = viewModel;
        Text = "Auction Setup";
        Width = 760;
        Height = 780;
        MinimumSize = new Size(680, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(242, 245, 248);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        buttons.Controls.Add(Button("New Auction", () => { _viewModel.NewAuction(); RefreshEditor(); }));
        buttons.Controls.Add(Button("Save", SaveAuction));
        buttons.Controls.Add(Button("Load Auction", LoadAuction));
        buttons.Controls.Add(Button("Close Auction", CloseAuction));
        buttons.Controls.Add(Button("Duplicate Previous Auction", () => { _viewModel.DuplicatePreviousAuction(); RefreshEditor(); }));
        buttons.Controls.Add(_copyConsignors);

        var heading=new Label { Text="Auction Setup",Font=new Font("Segoe UI",18F,FontStyle.Bold),ForeColor=Color.FromArgb(24,50,74),AutoSize=true,Margin=new Padding(8,8,8,12) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, Padding=new Padding(14) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(heading,0,0); layout.Controls.Add(_auctions,0,1); layout.Controls.Add(_editor,0,2); layout.Controls.Add(buttons,0,3);
        Controls.Add(layout);
        Load += (_, _) => { _viewModel.Load(); RefreshEditor(); };
    }

    private static Button Button(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Padding=new Padding(12,5,12,5),Margin=new Padding(4) };
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
