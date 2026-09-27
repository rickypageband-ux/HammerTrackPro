using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class SoldItemSearchForm : Form
{
    private readonly SoldItemSearchViewModel _viewModel;
    private readonly TextBox _query = new() { Dock = DockStyle.Fill, PlaceholderText = "Description (for example towels) or lot number" };
    private readonly DataGridView _results = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        AllowUserToAddRows = false, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false };
    private readonly NumericUpDown _bidder = new() { Minimum = 0, Maximum = 999999, Width = 130 };
    private readonly NumericUpDown _count = new() { Minimum = 1, Maximum = 999999, Value = 1, Width = 130 };
    private readonly Label _details = new() { Dock = DockStyle.Fill, AutoSize = false };
    private readonly Label _status = new() { Dock = DockStyle.Fill, ForeColor = Color.DarkRed };
    private readonly Button _record = new() { Text = "Add Buyer at This Price", AutoSize = true, Height = 42, Enabled = false };

    public SoldItemSearchForm(SoldItemSearchViewModel viewModel)
    {
        _viewModel = viewModel;
        Text = "HammerTrack Pro - Search Sold Items";
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(1000, 720); MinimumSize = new Size(850, 650);
        Font = new Font("Segoe UI", 11F);
        _results.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _results.ColumnHeadersHeight = 38;
        _results.RowTemplate.Height = 32;
        _results.BackgroundColor = Color.White;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), RowCount = 7, ColumnCount = 1 };
        foreach (var height in new[] { 56, 48, 0, 90, 58, 58, 48 })
            layout.RowStyles.Add(height == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, height));
        layout.Controls.Add(new Label { Text = "SEARCH SOLD ITEMS\nChoose the original lot and price. This adds a new purchase; previous buyers keep theirs.", Dock = DockStyle.Fill }, 0, 0);
        var searchRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); searchRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        var search = new Button { Text = "Search", Dock = DockStyle.Fill };
        search.Click += (_, _) => Search();
        _query.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Search(); } };
        searchRow.Controls.Add(_query); searchRow.Controls.Add(search); layout.Controls.Add(searchRow, 0, 1);
        AddColumn("Lot", nameof(SoldItem.LotNumber), 12);
        AddColumn("Description", nameof(SoldItem.Description), 48);
        AddColumn("Seller #", nameof(SoldItem.SellerNumber), 14);
        AddColumn("Price Each", nameof(SoldItem.PriceEach), 18, "C2");
        if (_viewModel.SaleMode == AuctionSaleMode.Wholesale) AddColumn("Qty / Deal", nameof(SoldItem.QuantityPerDeal), 18);
        _results.SelectionChanged += (_, _) => { _viewModel.Select(_results.CurrentRow?.DataBoundItem as SoldItem); _count.Value = 1; RefreshDetails(); };
        _results.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) _bidder.Focus(); };
        layout.Controls.Add(_results, 0, 2); layout.Controls.Add(_details, 0, 3);
        var entry = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        entry.Controls.Add(new Label { Text = "Bidder Number", AutoSize = true }); entry.Controls.Add(_bidder);
        entry.Controls.Add(new Label { Text = _viewModel.CountLabel, AutoSize = true }); entry.Controls.Add(_count);
        _count.ValueChanged += (_, _) => { _viewModel.QuantityOrDeals = (int)_count.Value; RefreshDetails(); };
        foreach (var number in new[] { _bidder, _count }) number.Enter += (_, _) => number.Select(0, number.Text.Length);
        layout.Controls.Add(entry, 0, 4);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _record.Click += (_, _) => Record();
        var cancel = new Button { Text = "Cancel", AutoSize = true, Height = 42, DialogResult = DialogResult.Cancel };
        actions.Controls.Add(_record); actions.Controls.Add(cancel); layout.Controls.Add(actions, 0, 5);
        layout.Controls.Add(_status, 0, 6); Controls.Add(layout); CancelButton = cancel; AcceptButton = _record;
        Shown += (_, _) => { Search(); _query.Focus(); };
    }

    private void AddColumn(string title, string property, int weight, string format = "") =>
        _results.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = title, DataPropertyName = property,
            FillWeight = weight, DefaultCellStyle = new DataGridViewCellStyle { Format = format } });

    private void Search()
    {
        try
        {
            _results.DataSource = null; _viewModel.Search(_query.Text); _results.DataSource = _viewModel.Results;
            _status.ForeColor = Color.DimGray;
            _status.Text = _viewModel.Results.Count == 0 ? "No matching sold items in this auction and mode." : "Up to 100 matches. Narrow your search if needed.";
            RefreshDetails();
        }
        catch (Exception exception) { _status.ForeColor = Color.DarkRed; _status.Text = exception.Message; }
    }

    private void RefreshDetails()
    {
        var item = _viewModel.SelectedItem; _record.Enabled = item is not null;
        _details.Text = item is null ? "Select a sold item above." :
            $"Lot #{item.LotNumber}: {item.Description}\nLocked price each: {item.PriceEach:C2}" +
            (_viewModel.SaleMode == AuctionSaleMode.Wholesale ? $" | Locked items per deal: {item.QuantityPerDeal}" : "") +
            $"\nNew hammer total: {_viewModel.Total:C2} (auction premium and tax apply at checkout).";
    }

    private void Record()
    {
        _record.Enabled = false;
        try { _viewModel.BidderNumber = (int)_bidder.Value; _viewModel.QuantityOrDeals = (int)_count.Value; _viewModel.Record(); DialogResult = DialogResult.OK; Close(); }
        catch (Exception exception) { _status.ForeColor = Color.DarkRed; _status.Text = exception.Message; _record.Enabled = _viewModel.SelectedItem is not null; }
    }
}
