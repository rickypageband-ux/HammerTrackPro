using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class MainForm : Form
{
    private readonly IAuctionService _service;
    private readonly ConsignorService _consignorService;
    private readonly ICashierService _cashierService;
    private readonly WorkstationConfiguration _workstationConfiguration;
    private readonly AuctionSetupViewModel _auctionSetup;
    private AppData _data;

    private readonly TabControl _tabs = new();
    private readonly Label _bidderCount = new();
    private readonly Label _lotCount = new();
    private readonly Label _soldCount = new();
    private readonly Label _grossSales = new();
    private readonly Label _averageSalePrice = new();
    private readonly TextBox _auctionName = new();
    private readonly DateTimePicker _auctionDate = new();
    private readonly TextBox _clerkName = new();
    private readonly TextBox _cashierName = new();
    private readonly ComboBox _auctionStatus = new();
    private readonly TextBox _auctionLocation = new();
    private readonly NumericUpDown _buyerPremium = new();
    private readonly NumericUpDown _defaultConsignorCommission = new();
    private readonly NumericUpDown _salesTax = new();
    private readonly ListBox _recentActivity = new();

    private readonly DataGridView _bidderGrid = new();
    private readonly DataGridView _lotGrid = new();
    private readonly TextBox _bidderSearch = new();
    private readonly TextBox _lotSearch = new();

    private readonly NumericUpDown _bidderNumber = new();
    private readonly ComboBox _existingBuyer = new() { DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(Buyer.FullName), ValueMember = nameof(Buyer.BuyerId) };
    private readonly TextBox _firstName = new();
    private readonly TextBox _lastName = new();
    private readonly TextBox _driversLicense = new();
    private readonly TextBox _address = new();
    private readonly TextBox _city = new();
    private readonly TextBox _state = new();
    private readonly TextBox _zip = new();
    private readonly TextBox _phone = new();
    private readonly TextBox _email = new();
    private readonly TextBox _notes = new();

    private readonly NumericUpDown _lotNumber = new();
    private readonly TextBox _description = new();
    private readonly TextBox _category = new();
    private readonly NumericUpDown _startingBid = new();
    private readonly ComboBox _status = new();
    private readonly ComboBox _lotConsignor = new();
    private readonly NumericUpDown _reservePrice = new();
    private readonly CheckBox _passOutMode = new() { Text = "Enabled", AutoSize = true };

    private int? _editingBidderNumber;
    private int? _editingLotNumber;

    public MainForm(IAuctionService service, ConsignorService consignorService, ICashierService cashierService,WorkstationConfiguration workstationConfiguration)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _consignorService = consignorService ?? throw new ArgumentNullException(nameof(consignorService));
        _cashierService = cashierService ?? throw new ArgumentNullException(nameof(cashierService));
        _workstationConfiguration=workstationConfiguration;
        _auctionSetup = new AuctionSetupViewModel(_service);
        _auctionSetup.Load();
        _data = _service.Load();
        Text = "HammerTrack Pro";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Width = 1280;
        Height = 800;
        Font = new Font("Segoe UI", 10F);

        Controls.Add(BuildLayout());
        RefreshAll();
        NewBidder();
        NewLot();
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 50, 74) };
        var title = new Label
        {
            Text = "HAMMERTRACK PRO",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            AutoSize = true,
            Left = 20,
            Top = 15
        };
        var backup = new Button { Text = "Backup Database", Width = 150, Height = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        backup.Left = 1080;
        backup.Top = 15;
        backup.Click += (_, _) => BackupDatabase();
        var network=new Button { Text="Network Information",Width=160,Height=34,Top=15,Anchor=AnchorStyles.Top|AnchorStyles.Right };
        network.Left=backup.Left-network.Width-10;
        network.Click+=(_,_)=>{ using var info=new NetworkInformationForm(_workstationConfiguration); info.ShowDialog(this); };
        header.Resize += (_, _) => { backup.Left=header.ClientSize.Width-backup.Width-20; network.Left=backup.Left-network.Width-10; };
        header.Controls.Add(title);
        header.Controls.Add(backup);
        header.Controls.Add(network);

        _tabs.Dock = DockStyle.Fill;
        _tabs.TabPages.Add(BuildDashboardTab());
        _tabs.TabPages.Add(BuildBiddersTab());
        _tabs.TabPages.Add(BuildLotsTab());
        _tabs.SelectedIndexChanged += (_, _) => RefreshAll();

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(_tabs, 0, 1);
        return root;
    }

    private TabPage BuildDashboardTab()
    {
        var tab = new TabPage("Dashboard") { BackColor = Color.FromArgb(242, 245, 248) };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(22) };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(4)
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var heading = new Label
        {
            Text = "Auction Dashboard",
            AutoSize = true,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 50, 74),
            Margin = new Padding(8, 4, 8, 14)
        };
        content.Controls.Add(heading, 0, 0);

        var cards = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 5, Margin = new Padding(0, 0, 0, 16) };
        for (var i = 0; i < 5; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        cards.Controls.Add(BuildCard("Registered Bidders", _bidderCount), 0, 0);
        cards.Controls.Add(BuildCard("Total Lots", _lotCount), 1, 0);
        cards.Controls.Add(BuildCard("Lots Sold", _soldCount), 2, 0);
        cards.Controls.Add(BuildCard("Gross Sales", _grossSales), 3, 0);
        cards.Controls.Add(BuildCard("Average Sale", _averageSalePrice), 4, 0);
        content.Controls.Add(cards, 0, 1);

        var middle = new TableLayoutPanel { Dock = DockStyle.Top, Height = 430, ColumnCount = 2, Margin = new Padding(0, 0, 0, 16) };
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        middle.Controls.Add(BuildAuctionInformationPanel(), 0, 0);
        middle.Controls.Add(BuildQuickActionsPanel(), 1, 0);
        content.Controls.Add(middle, 0, 2);
        content.Controls.Add(BuildRecentActivityPanel(), 0, 3);

        scroll.Controls.Add(content);
        tab.Controls.Add(scroll);
        return tab;
    }

    private static Control BuildCard(string caption, Label valueLabel)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 135,
            Margin = new Padding(8),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        var captionLabel = new Label
        {
            Text = caption,
            AutoSize = true,
            Left = 16,
            Top = 16,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 90, 100)
        };
        valueLabel.AutoSize = true;
        valueLabel.Left = 16;
        valueLabel.Top = 48;
        valueLabel.Font = new Font("Segoe UI", 23F, FontStyle.Bold);
        valueLabel.ForeColor = Color.FromArgb(24, 50, 74);
        card.Controls.Add(captionLabel);
        card.Controls.Add(valueLabel);
        return card;
    }

    private Control BuildAuctionInformationPanel()
    {
        var group = new GroupBox
        {
            Text = "Auction Information",
            Dock = DockStyle.Fill,
            Height = 420,
            Padding = new Padding(14),
            Margin = new Padding(8)
        };
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 0, AutoScroll = true, Padding = new Padding(4) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(table, "Auction Name", _auctionName);
        _auctionDate.Format = DateTimePickerFormat.Long;
        AddRow(table, "Auction Date", _auctionDate);
        AddRow(table, "Location", _auctionLocation);
        ConfigurePercent(_buyerPremium); AddRow(table, "Buyer Premium %", _buyerPremium);
        ConfigurePercent(_defaultConsignorCommission); AddRow(table, "Seller Commission %", _defaultConsignorCommission);
        ConfigurePercent(_salesTax); AddRow(table, "Sales Tax %", _salesTax);
        AddRow(table, "Clerk", _clerkName);
        AddRow(table, "Cashier", _cashierName);
        _auctionStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        _auctionStatus.Items.AddRange(new object[] { "Setup", "Live", "Closed" });
        AddRow(table, "Status", _auctionStatus);
        var save = MakeButton("Save Auction Information", (_, _) => SaveAuctionSettings());
        save.BackColor = Color.FromArgb(24, 91, 140);
        save.ForeColor = Color.White;
        save.FlatStyle = FlatStyle.Flat;
        table.Controls.Add(save, 1, table.RowCount++);
        group.Controls.Add(table);
        return group;
    }

    private Control BuildQuickActionsPanel()
    {
        var group = new GroupBox
        {
            Text = "Quick Actions",
            Dock = DockStyle.Fill,
            Height = 420,
            Padding = new Padding(14),
            Margin = new Padding(8)
        };
        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (var row = 0; row < 4; row++) buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        buttons.Controls.Add(MakeDashboardButton("Register Bidder", (_, _) => { _tabs.SelectedIndex = 1; NewBidder(); }), 0, 0);
        buttons.Controls.Add(MakeDashboardButton("Add Lot", (_, _) => { _tabs.SelectedIndex = 2; NewLot(); }), 1, 0);
        buttons.Controls.Add(MakeDashboardButton("Backup Database", (_, _) => BackupDatabase()), 0, 1);
        buttons.Controls.Add(MakeDashboardButton("Refresh Dashboard", (_, _) => RefreshAll()), 1, 1);
        buttons.Controls.Add(MakeDashboardButton("Open Live Auction", (_, _) => OpenLiveAuction()), 0, 2);
        buttons.Controls.Add(MakeDashboardButton("Manage Consignors", (_, _) => OpenConsignors()), 1, 2);
        buttons.Controls.Add(MakeDashboardButton("Auction Setup", (_, _) => OpenAuctionSetup()), 0, 3);
        buttons.Controls.Add(MakeDashboardButton("Cashier", (_, _) => OpenCashier()), 1, 3);
        group.Controls.Add(buttons);
        return group;
    }

    private Control BuildRecentActivityPanel()
    {
        var group = new GroupBox
        {
            Text = "Recent Activity",
            Dock = DockStyle.Top,
            Height = 210,
            Padding = new Padding(12),
            Margin = new Padding(8)
        };
        _recentActivity.Dock = DockStyle.Fill;
        _recentActivity.BorderStyle = BorderStyle.None;
        _recentActivity.Font = new Font("Segoe UI", 10F);
        group.Controls.Add(_recentActivity);
        return group;
    }

    private static Button MakeDashboardButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(7),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(24, 50, 74),
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(185, 195, 205);
        button.Click += onClick;
        return button;
    }

    private TabPage BuildBiddersTab()
    {
        var tab = new TabPage("Bidders");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 430, FixedPanel = FixedPanel.Panel1 };

        var editor = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(4) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(form, "Bidder Number", _bidderNumber);
        _bidderNumber.Maximum = 999999;
        AddRow(form, "Existing Buyer", _existingBuyer);
        _existingBuyer.SelectedIndexChanged += (_, _) => LoadExistingBuyer();
        AddRow(form, "First Name", _firstName);
        AddRow(form, "Last Name", _lastName);
        AddRow(form, "Driver's License", _driversLicense);
        AddRow(form, "Address", _address);
        AddRow(form, "City", _city);
        AddRow(form, "State", _state);
        AddRow(form, "ZIP", _zip);
        AddRow(form, "Phone", _phone);
        AddRow(form, "Email", _email);
        _notes.Multiline = true;
        _notes.Height = 80;
        AddRow(form, "Notes", _notes);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        buttons.Controls.Add(MakeButton("New", (_, _) => NewBidder()));
        buttons.Controls.Add(MakeButton("Save", (_, _) => SaveBidder()));
        buttons.Controls.Add(MakeButton("Delete", (_, _) => DeleteBidder()));
        form.Controls.Add(buttons, 1, form.RowCount);
        editor.Controls.Add(form);
        split.Panel1.Controls.Add(editor);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var searchBar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _bidderSearch.Width = 320;
        searchBar.Controls.Add(_bidderSearch);
        searchBar.Controls.Add(MakeButton("Search", (_, _) => RefreshBidders()));
        _bidderSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { RefreshBidders(); e.SuppressKeyPress = true; } };

        ConfigureGrid(_bidderGrid);
        _bidderGrid.SelectionChanged += (_, _) => LoadSelectedBidder();
        right.Controls.Add(searchBar, 0, 0);
        right.Controls.Add(_bidderGrid, 0, 1);
        split.Panel2.Controls.Add(right);
        tab.Controls.Add(split);
        return tab;
    }

    private TabPage BuildLotsTab()
    {
        var tab = new TabPage("Lots");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 400, FixedPanel = FixedPanel.Panel1 };

        var editor = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(4) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _lotNumber.Maximum = 999999;
        AddRow(form, "Lot Number", _lotNumber);
        _description.Multiline = true;
        _description.Height = 90;
        AddRow(form, "Description", _description);
        AddRow(form, "Category", _category);
        _lotConsignor.DropDownStyle = ComboBoxStyle.DropDownList;
        _lotConsignor.DisplayMember = nameof(Consignor.LotDisplayName); _lotConsignor.ValueMember = nameof(Consignor.ConsignorId);
        _lotConsignor.DropDownWidth = 360;
        AddRow(form, "Consignor", _lotConsignor);
        _startingBid.DecimalPlaces = 2;
        _startingBid.Maximum = 100000000;
        AddRow(form, "Starting Bid", _startingBid);
        _reservePrice.DecimalPlaces = 2; _reservePrice.Maximum = 100000000; AddRow(form, "Reserve Price", _reservePrice);
        AddRow(form, "Pass-Out Mode", _passOutMode);
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[] { "Available", "Sold", "Withdrawn" });
        AddRow(form, "Status", _status);

        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        buttons.Controls.Add(MakeButton("New", (_, _) => NewLot()));
        buttons.Controls.Add(MakeButton("Save", (_, _) => SaveLot()));
        buttons.Controls.Add(MakeButton("Delete", (_, _) => DeleteLot()));
        form.Controls.Add(buttons, 1, form.RowCount);
        editor.Controls.Add(form);
        split.Panel1.Controls.Add(editor);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(8) };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var searchBar = new FlowLayoutPanel { Dock = DockStyle.Fill };
        _lotSearch.Width = 320;
        searchBar.Controls.Add(_lotSearch);
        searchBar.Controls.Add(MakeButton("Search", (_, _) => RefreshLots()));
        _lotSearch.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { RefreshLots(); e.SuppressKeyPress = true; } };

        ConfigureGrid(_lotGrid);
        _lotGrid.SelectionChanged += (_, _) => LoadSelectedLot();
        right.Controls.Add(searchBar, 0, 0);
        right.Controls.Add(_lotGrid, 0, 1);
        split.Panel2.Controls.Add(right);
        tab.Controls.Add(split);
        return tab;
    }

    private static void AddRow(TableLayoutPanel table, string labelText, Control control)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label { Text = labelText, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 9, 3, 3) };
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 4, 3, 4);
        table.Controls.Add(label, 0, row);
        table.Controls.Add(control, 1, row);
    }

    private static void ConfigurePercent(NumericUpDown control) { control.DecimalPlaces = 2; control.Maximum = 100; control.Increment = 0.25m; }

    private static Button MakeButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true, Padding = new Padding(10, 4, 10, 4), Margin = new Padding(4) };
        button.Click += onClick;
        return button;
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoGenerateColumns = true;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(24, 50, 74);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(214, 232, 245);
        grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 50, 74);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 249, 251);
    }

    private void RefreshAll()
    {
        _auctionSetup.Load();
        _data = _service.Load();
        RefreshConsignorChoices();
        RefreshBidders();
        RefreshLots();
        RefreshDashboard();
    }

    private void RefreshDashboard()
    {
        var snapshot = _service.GetDashboardSnapshot();
        _bidderCount.Text = snapshot.BidderCount.ToString("N0");
        _lotCount.Text = snapshot.LotCount.ToString("N0");
        _soldCount.Text = snapshot.SoldCount.ToString("N0");
        _grossSales.Text = snapshot.GrossSales.ToString("C2");
        _averageSalePrice.Text = snapshot.AverageSalePrice.ToString("C2");

        _recentActivity.BeginUpdate();
        _recentActivity.Items.Clear();
        if (snapshot.RecentActivity.Count == 0)
            _recentActivity.Items.Add("No activity has been recorded yet.");
        else
            _recentActivity.Items.AddRange(snapshot.RecentActivity.Cast<object>().ToArray());
        _recentActivity.EndUpdate();

        if (!_auctionName.Focused) _auctionName.Text = _service.GetSetting("AuctionName", "New Auction");
        var auction = _auctionSetup.CurrentAuction;
        if (!_auctionName.Focused) _auctionName.Text = auction.AuctionName;
        if (!_auctionDate.Focused) _auctionDate.Value = auction.AuctionDate;
        if (!_auctionLocation.Focused) _auctionLocation.Text = auction.Location;
        if (!_buyerPremium.Focused) _buyerPremium.Value = auction.BuyerPremiumPercent;
        if (!_defaultConsignorCommission.Focused) _defaultConsignorCommission.Value = auction.DefaultConsignorCommissionPercent;
        if (!_salesTax.Focused) _salesTax.Value = auction.SalesTaxPercent;
        if (!_clerkName.Focused) _clerkName.Text = auction.Clerk;
        if (!_cashierName.Focused) _cashierName.Text = auction.Cashier;
        if (!_auctionStatus.Focused) _auctionStatus.SelectedItem = auction.Status;
    }

    private void SaveAuctionSettings()
    {
        var auction = _auctionSetup.CurrentAuction;
        auction.AuctionName = _auctionName.Text.Trim(); auction.AuctionDate = _auctionDate.Value.Date; auction.Location = _auctionLocation.Text.Trim();
        auction.BuyerPremiumPercent = _buyerPremium.Value; auction.DefaultConsignorCommissionPercent = _defaultConsignorCommission.Value; auction.SalesTaxPercent = _salesTax.Value;
        auction.Clerk = _clerkName.Text.Trim(); auction.Cashier = _cashierName.Text.Trim();
        _auctionSetup.Save();
        _service.SaveSettings(new Dictionary<string, string>
        {
            ["AuctionName"] = _auctionName.Text.Trim(),
            ["AuctionDate"] = _auctionDate.Value.ToString("yyyy-MM-dd"),
            ["ClerkName"] = _clerkName.Text.Trim(),
            ["CashierName"] = _cashierName.Text.Trim(),
            ["AuctionStatus"] = _auctionStatus.SelectedItem?.ToString() ?? "Setup"
        });
        MessageBox.Show("Auction information saved.", "HammerTrack Pro", MessageBoxButtons.OK, MessageBoxIcon.Information);
        RefreshDashboard();
    }

    private void OpenLiveAuction()
    {
        using var liveAuction = new LiveAuctionForm(_service);
        liveAuction.ShowDialog(this);
        RefreshAll();
    }

    private void OpenConsignors()
    {
        using var consignors = new ConsignorForm(_consignorService);
        consignors.ShowDialog(this);
        RefreshAll();
    }

    private void OpenAuctionSetup()
    {
        using var setup = new AuctionSetupForm(new AuctionSetupViewModel(_service));
        setup.ShowDialog(this);
        _auctionSetup.Load();
        RefreshAll();
    }

    private void OpenCashier()
    {
        using var cashier=new CashierForm(new CashierViewModel(_cashierService),_cashierService);
        cashier.ShowDialog(this); RefreshAll();
    }

    private static void ShowUpcoming(string moduleName)
    {
        MessageBox.Show($"{moduleName} will be added in the next milestone.", "Coming Next", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RefreshBidders()
    {
        var q = _bidderSearch.Text.Trim();
        _bidderGrid.DataSource = _data.Bidders
            .Where(b => q.Length == 0 || b.BidderNumber.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.DriversLicense.Contains(q, StringComparison.OrdinalIgnoreCase)
                || b.Phone.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(b => b.BidderNumber)
            .ToList();
    }

    private void RefreshLots()
    {
        var q = _lotSearch.Text.Trim();
        _lotGrid.DataSource = _data.Lots
            .Where(l => q.Length == 0 || l.LotNumber.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                || l.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
                || l.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(l => l.LotNumber)
            .ToList();
    }

    private void NewBidder()
    {
        _editingBidderNumber = null;
        _existingBuyer.DataSource = new List<Buyer> { new() { BuyerId = 0, FirstName = "(Create new buyer)" } }.Concat(_service.GetBuyers()).ToList();
        _existingBuyer.SelectedIndex = 0;
        _bidderNumber.Value = _data.Bidders.Count == 0 ? 1 : _data.Bidders.Max(b => b.BidderNumber) + 1;
        _firstName.Clear(); _lastName.Clear(); _driversLicense.Clear(); _address.Clear(); _city.Clear();
        _state.Clear(); _zip.Clear(); _phone.Clear(); _email.Clear(); _notes.Clear();
        _firstName.Focus();
    }

    private void SaveBidder()
    {
        var number = Decimal.ToInt32(_bidderNumber.Value);
        if (string.IsNullOrWhiteSpace(_firstName.Text) && string.IsNullOrWhiteSpace(_lastName.Text))
        {
            MessageBox.Show("Enter the bidder's name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_data.Bidders.Any(b => b.BidderNumber == number && b.BidderNumber != _editingBidderNumber))
        {
            MessageBox.Show("That bidder number is already in use.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var bidder = _editingBidderNumber is null
            ? new AuctionBidder { AuctionId = _auctionSetup.CurrentAuction.AuctionId }
            : _data.Bidders.First(b => b.BidderNumber == _editingBidderNumber.Value);

        bidder.BuyerId = _existingBuyer.SelectedValue is long buyerId ? buyerId : bidder.BuyerId;
        bidder.BidderNumber = number;
        bidder.FirstName = _firstName.Text.Trim();
        bidder.LastName = _lastName.Text.Trim();
        bidder.DriversLicense = _driversLicense.Text.Trim();
        bidder.Address = _address.Text.Trim();
        bidder.City = _city.Text.Trim();
        bidder.State = _state.Text.Trim();
        bidder.Zip = _zip.Text.Trim();
        bidder.Phone = _phone.Text.Trim();
        bidder.Email = _email.Text.Trim();
        bidder.Notes = _notes.Text.Trim();

        _service.SaveBidder(bidder, _editingBidderNumber);
        RefreshAll();
        NewBidder();
    }

    private void DeleteBidder()
    {
        if (_editingBidderNumber is null) return;
        var bidder = _data.Bidders.FirstOrDefault(b => b.BidderNumber == _editingBidderNumber.Value);
        if (bidder is null) return;
        if (MessageBox.Show($"Delete bidder {bidder.BidderNumber} - {bidder.FullName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.DeleteBidder(bidder.BidderNumber);
        RefreshAll();
        NewBidder();
    }

    private void LoadSelectedBidder()
    {
        if (_bidderGrid.CurrentRow?.DataBoundItem is not AuctionBidder bidder) return;
        _editingBidderNumber = bidder.BidderNumber;
        _existingBuyer.DataSource = new List<Buyer> { new() { BuyerId=0, FirstName="(Create new buyer)" } }.Concat(_service.GetBuyers()).ToList();
        _existingBuyer.SelectedValue = bidder.BuyerId;
        _bidderNumber.Value = bidder.BidderNumber;
        _firstName.Text = bidder.FirstName;
        _lastName.Text = bidder.LastName;
        _driversLicense.Text = bidder.DriversLicense;
        _address.Text = bidder.Address;
        _city.Text = bidder.City;
        _state.Text = bidder.State;
        _zip.Text = bidder.Zip;
        _phone.Text = bidder.Phone;
        _email.Text = bidder.Email;
        _notes.Text = bidder.Notes;
    }

    private void LoadExistingBuyer()
    {
        if (_existingBuyer.SelectedItem is not Buyer buyer || buyer.BuyerId == 0) return;
        _firstName.Text=buyer.FirstName; _lastName.Text=buyer.LastName; _driversLicense.Text=buyer.DriversLicense;
        _address.Text=buyer.Address; _city.Text=buyer.City; _state.Text=buyer.State; _zip.Text=buyer.Zip;
        _phone.Text=buyer.Phone; _email.Text=buyer.Email;
    }

    private void NewLot()
    {
        _editingLotNumber = null;
        _lotNumber.Value = _data.Lots.Count == 0 ? 1 : _data.Lots.Max(l => l.LotNumber) + 1;
        _description.Clear();
        _category.Clear();
        _startingBid.Value = 0;
        _reservePrice.Value = 0; _passOutMode.Checked = false;
        RefreshConsignorChoices();
        _status.SelectedIndex = 0;
        _description.Focus();
    }

    private void RefreshConsignorChoices()
    {
        var selectedId=_lotConsignor.SelectedValue is long id ? id : (long?)null;
        var consignors=_consignorService.GetConsignors().OrderByDescending(item=>item.Active).ThenBy(item=>item.ConsignorNumber).ToList();
        _lotConsignor.DataSource=null; _lotConsignor.DataSource=consignors;
        if(selectedId.HasValue && consignors.Any(item=>item.ConsignorId==selectedId.Value)) _lotConsignor.SelectedValue=selectedId.Value;
    }

    private void SaveLot()
    {
        var number = Decimal.ToInt32(_lotNumber.Value);
        if (string.IsNullOrWhiteSpace(_description.Text))
        {
            MessageBox.Show("Enter a lot description.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_data.Lots.Any(l => l.LotNumber == number && l.LotNumber != _editingLotNumber))
        {
            MessageBox.Show("That lot number is already in use.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var lot = _editingLotNumber is null
            ? new AuctionLot()
            : _data.Lots.First(l => l.LotNumber == _editingLotNumber.Value);

        lot.LotNumber = number;
        lot.Description = _description.Text.Trim();
        lot.Category = _category.Text.Trim();
        lot.StartingBid = _startingBid.Value;
        lot.Status = _status.SelectedItem?.ToString() ?? "Available";
        lot.LotStatus = lot.Status; lot.AuctionId = _auctionSetup.CurrentAuction.AuctionId;
        lot.ConsignorId = _lotConsignor.SelectedValue is long id ? id : throw new InvalidOperationException("Select a consignor.");
        lot.ReservePrice = _reservePrice.Value; lot.PassOutMode = _passOutMode.Checked;

        _service.SaveLot(lot, _editingLotNumber);
        RefreshAll();
        NewLot();
    }

    private void DeleteLot()
    {
        if (_editingLotNumber is null) return;
        var lot = _data.Lots.FirstOrDefault(l => l.LotNumber == _editingLotNumber.Value);
        if (lot is null) return;
        if (MessageBox.Show($"Delete lot {lot.LotNumber} - {lot.Description}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _service.DeleteLot(lot.LotNumber);
        RefreshAll();
        NewLot();
    }

    private void LoadSelectedLot()
    {
        if (_lotGrid.CurrentRow?.DataBoundItem is not AuctionLot lot) return;
        _editingLotNumber = lot.LotNumber;
        _lotNumber.Value = lot.LotNumber;
        _description.Text = lot.Description;
        _category.Text = lot.Category;
        _startingBid.Value = Math.Min(_startingBid.Maximum, Math.Max(_startingBid.Minimum, lot.StartingBid));
        _status.SelectedItem = lot.Status;
        _lotConsignor.DataSource = _consignorService.GetConsignors().OrderBy(x => x.ConsignorNumber).ToList(); _lotConsignor.SelectedValue = lot.ConsignorId;
        _reservePrice.Value = Math.Min(_reservePrice.Maximum, lot.ReservePrice); _passOutMode.Checked = lot.PassOutMode;
        if (_status.SelectedIndex < 0) _status.SelectedIndex = 0;
    }

    private void BackupDatabase()
    {
        try
        {
            var path = _service.Backup();
            MessageBox.Show($"Backup created successfully:\n{path}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed:\n{ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
