using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class LiveAuctionForm : Form
{
    private readonly IAuctionService _service;
    private readonly LiveAuctionViewModel _viewModel;
    private readonly NumericUpDown _quickConsignorNumber = new();
    private readonly TextBox _quickDescription = new();
    private readonly NumericUpDown _lotNumber = new();
    private readonly NumericUpDown _bidderNumber = new();
    private readonly NumericUpDown _hammerPrice = new();
    private readonly NumericUpDown _quantity = new();
    private readonly NumericUpDown _deals = new();
    private readonly NumericUpDown _queuedLot = new();
    private readonly CheckBox _passOutMode = new();
    private readonly Label _heroLot = new();
    private readonly Label _heroDescription = new();
    private readonly Label _heroPrice = new();
    private readonly Label _buyerCount = new();
    private readonly Label _status = new();
    private readonly Label _previousSale = new();
    private readonly DataGridView _salesGrid = new();
    private readonly Label _historyDetails = new();
    private readonly Button _editSale = new();
    private readonly Label _modeLabel = new();
    private readonly TextBox _notes = new();
    private readonly Button _completeSale = new();
    private readonly Button _closeLot = new();
    private readonly Button _passLot = new();
    private readonly Button _undoLastSale = new();
    private readonly Button _fullScreenButton = new();
    private readonly Button _createLot = new();
    private Sale? _lastSale;
    private int _lastBidderNumber;
    private bool _fullScreen;
    private FormBorderStyle _savedBorderStyle;
    private FormWindowState _savedWindowState;
    private bool _focusBidderWhenShown;
    private bool IsWholesale=>_viewModel.SaleMode==AuctionSaleMode.Wholesale;

    public LiveAuctionForm(IAuctionService service,LiveAuctionViewModel viewModel)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));
        Text = IsWholesale ? "HammerTrack Pro - Wholesale Auction" : "HammerTrack Pro - Retail Auction";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1000, 760);
        Width = 1180;
        Height = 860;
        Font = new Font("Segoe UI", 12F);
        KeyPreview = true;
        KeyDown += HandleFormKeyDown;
        Controls.Add(BuildLayout());
        Shown+=(_,_)=>
        {
            if(!_focusBidderWhenShown)return;
            _focusBidderWhenShown=false;
            FocusBidderEntry();
        };
        AcceptButton = _completeSale;
        UpdateModeDisplay();
        LoadLastSale();
        LoadNextAvailableLot();
    }

    private Control BuildLayout()
    {
        var shell=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(12),BackColor=Color.FromArgb(242,245,248)};
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,68));shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,32));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(4,4,10,4),
            BackColor = Color.FromArgb(242, 245, 248)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeroPanel(), 0, 0);

        _modeLabel.Text = "STANDARD LOT — one buyer";
        _modeLabel.Dock = DockStyle.Top;
        _modeLabel.Height = 38;
        _modeLabel.TextAlign = ContentAlignment.MiddleCenter;
        _modeLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _modeLabel.BackColor = Color.FromArgb(225, 230, 235);
        _modeLabel.ForeColor = Color.FromArgb(45, 55, 65);
        root.Controls.Add(_modeLabel, 0, 1);

        _status.Text = "Ready";
        _status.Dock = DockStyle.Top;
        _status.Height = 46;
        _status.TextAlign = ContentAlignment.MiddleCenter;
        _status.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        _status.BackColor = Color.FromArgb(220, 235, 245);
        _status.ForeColor = Color.FromArgb(24, 50, 74);
        root.Controls.Add(_status, 0, 2);

        root.Controls.Add(BuildQuickAddPanel(), 0, 3);
        root.Controls.Add(BuildEntryCard(), 0, 4);
        root.Controls.Add(BuildNotesAndQueuePanel(), 0, 5);

        var help = new Label
        {
            Text = "Enter: record buyer    F2: same buyer    F3: next lot    F4: pass item    F8: undo    F11: fullscreen    F12: close lot    Esc: exit fullscreen/close",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 10, 0, 0)
        };
        root.Controls.Add(help, 0, 6);
        shell.Controls.Add(root,0,0);shell.Controls.Add(BuildRecordedSalesPanel(),1,0);
        return shell;
    }

    private Control BuildQuickAddPanel()
    {
        var panel=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=3,RowCount=2,Padding=new Padding(12),Margin=new Padding(0,8,0,0),BackColor=Color.White };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,230));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,160));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent,50));panel.RowStyles.Add(new RowStyle(SizeType.Percent,50));
        panel.Controls.Add(new Label { Text="Quick Add Seller Number",AutoSize=true,Anchor=AnchorStyles.Left,Font=new Font("Segoe UI",11F,FontStyle.Bold) },0,0);
        _quickConsignorNumber.Minimum=0; _quickConsignorNumber.Maximum=999999; _quickConsignorNumber.Dock=DockStyle.Fill;
        _quickConsignorNumber.TabIndex=0;
        panel.Controls.Add(_quickConsignorNumber,1,0);
        panel.Controls.Add(new Label { Text="Lot Description / Name",AutoSize=true,Anchor=AnchorStyles.Left,Font=new Font("Segoe UI",11F,FontStyle.Bold) },0,1);
        _quickDescription.Dock=DockStyle.Fill;
        _quickDescription.TabIndex=1;
        _quickDescription.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){QuickAddLot();e.SuppressKeyPress=true;}};
        panel.Controls.Add(_quickDescription,1,1);panel.SetColumnSpan(_quickDescription,2);
        ConfigureButton(_createLot,"CREATE LOT",(_,_)=>QuickAddLot(),true);_createLot.MinimumSize=new Size(150,48);_createLot.Width=150;_createLot.TabStop=false;panel.Controls.Add(_createLot,2,0);
        return panel;
    }

    private Control BuildHeroPanel()
    {
        var hero = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Color.FromArgb(24, 50, 74),
            Padding = new Padding(18)
        };
        hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        hero.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        hero.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

        _heroLot.Text = "LOT —";
        _heroLot.Dock = DockStyle.Fill;
        _heroLot.TextAlign = ContentAlignment.MiddleLeft;
        _heroLot.Font = new Font("Segoe UI", 52F, FontStyle.Bold);
        _heroLot.ForeColor = Color.White;
        hero.Controls.Add(_heroLot, 0, 0);

        _heroPrice.Text = "$0.00";
        _heroPrice.Dock = DockStyle.Fill;
        _heroPrice.TextAlign = ContentAlignment.MiddleCenter;
        _heroPrice.Font = new Font("Segoe UI", 34F, FontStyle.Bold);
        _heroPrice.ForeColor = Color.FromArgb(255, 220, 120);
        hero.Controls.Add(_heroPrice, 1, 0);

        _buyerCount.Text = "0\nBUYERS";
        _buyerCount.Dock = DockStyle.Fill;
        _buyerCount.TextAlign = ContentAlignment.MiddleCenter;
        _buyerCount.Font = new Font("Segoe UI", 24F, FontStyle.Bold);
        _buyerCount.ForeColor = Color.White;
        hero.Controls.Add(_buyerCount, 2, 0);

        _heroDescription.Text = "No lot selected";
        _heroDescription.Dock = DockStyle.Fill;
        _heroDescription.TextAlign = ContentAlignment.MiddleLeft;
        _heroDescription.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        _heroDescription.ForeColor = Color.WhiteSmoke;
        hero.SetColumnSpan(_heroDescription, 3);
        hero.Controls.Add(_heroDescription, 0, 1);
        return hero;
    }

    private Control BuildEntryCard()
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 12, 0, 12),MinimumSize=new Size(0,IsWholesale?465:400) };
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = IsWholesale?7:6 };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        if(IsWholesale)form.RowStyles.Add(new RowStyle(SizeType.Absolute,62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        ConfigureNumber(_lotNumber, 999999, 0);
        ConfigureNumber(_bidderNumber, 999999, 0);
        ConfigureNumber(_hammerPrice, 100000000, 2);
        ConfigureNumber(_quantity, 1000000, 0);_quantity.Minimum=1;_quantity.Value=1;
        ConfigureNumber(_deals,1000000,0);_deals.Minimum=1;_deals.Value=1;
        _hammerPrice.ThousandsSeparator = true;
        _lotNumber.TabStop=false;
        _hammerPrice.TabIndex=0;_bidderNumber.TabIndex=1;_quantity.TabIndex=2;_deals.TabIndex=3;
        SelectAllOnFocus(_hammerPrice);SelectAllOnFocus(_bidderNumber);SelectAllOnFocus(_quantity);SelectAllOnFocus(_deals);

        _lotNumber.ValueChanged += (_, _) => UpdateLotDisplay(true);
        _lotNumber.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { _hammerPrice.Focus(); _hammerPrice.Select(0, _hammerPrice.Text.Length); e.SuppressKeyPress = true; } };
        _hammerPrice.ValueChanged += (_, _) => UpdateHeroPrice();
        _hammerPrice.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { _bidderNumber.Focus();_bidderNumber.Select(0,_bidderNumber.Text.Length);e.SuppressKeyPress=true; } };
        _bidderNumber.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            var next=IsWholesale&&_passOutMode.Checked?_deals:_quantity;next.Focus();next.Select(0,next.Text.Length);
            e.SuppressKeyPress = true;
        };
        _quantity.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){if(IsWholesale&&_passOutMode.Checked){_deals.Focus();_deals.Select(0,_deals.Text.Length);}else CompleteSale();e.SuppressKeyPress=true;}};
        _deals.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){CompleteSale();e.SuppressKeyPress=true;}};

        _passOutMode.Text = "PASS-OUT MODE — keep this lot and price open for more buyers";
        _passOutMode.AutoSize = true;
        _passOutMode.TabStop=false;
        _passOutMode.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _passOutMode.CheckedChanged += (_, _) => UpdateModeDisplay();

        AddEntryRow(form, 0, "Lot Number", _lotNumber);
        AddEntryRow(form, 1, "Hammer Price (each)", _hammerPrice);
        AddEntryRow(form, 2, "Bidder Number", _bidderNumber);
        AddEntryRow(form, 3, "Quantity", _quantity);
        var passOutRow=4;
        if(IsWholesale){AddEntryRow(form,4,"Deals",_deals);passOutRow=5;}
        form.Controls.Add(new Label(), 0, passOutRow);
        form.Controls.Add(_passOutMode, 1, passOutRow);

        var buttons = new TableLayoutPanel { Dock=DockStyle.Top,Height=58,MaximumSize=new Size(0,58),ColumnCount=5,RowCount=1,Margin=new Padding(0,4,0,0) };
        for(var column=0;column<5;column++)buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));
        ConfigureButton(_completeSale, "RECORD BUYER  (Enter)", (_, _) => CompleteSale(), true);
        ConfigureButton(_closeLot, "Close Lot  (F12)", (_, _) => CloseCurrentLot());
        ConfigureButton(_passLot, "Pass Item / Next  (F4)", (_, _) => PassCurrentLot());
        ConfigureButton(_undoLastSale, "Undo Last Buyer  (F8)", (_, _) => UndoLastSale());
        ConfigureButton(_fullScreenButton, "Fullscreen  (F11)", (_, _) => ToggleFullScreen());
        foreach(var button in new[]{_completeSale,_passLot,_closeLot,_undoLastSale,_fullScreenButton})button.TabStop=false;
        buttons.Controls.Add(_completeSale,0,0);buttons.Controls.Add(_passLot,1,0);buttons.Controls.Add(_closeLot,2,0);buttons.Controls.Add(_undoLastSale,3,0);buttons.Controls.Add(_fullScreenButton,4,0);
        form.SetColumnSpan(buttons,2);form.Controls.Add(buttons,0,passOutRow+1);
        card.Controls.Add(form);
        return card;
    }

    private Control BuildRecordedSalesPanel()
    {
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(12),Margin=new Padding(4),RowCount=4,ColumnCount=1};panel.RowStyles.Add(new RowStyle(SizeType.Absolute,52));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,100));panel.RowStyles.Add(new RowStyle(SizeType.Absolute,58));
        var caption=new Label{Text="LIVE SALE HISTORY",Dock=DockStyle.Fill,Font=new Font("Segoe UI",14F,FontStyle.Bold),ForeColor=Color.FromArgb(24,50,74),TextAlign=ContentAlignment.MiddleLeft};panel.Controls.Add(caption,0,0);
        _salesGrid.Dock=DockStyle.Fill;_salesGrid.ReadOnly=true;_salesGrid.AllowUserToAddRows=false;_salesGrid.AllowUserToDeleteRows=false;_salesGrid.AllowUserToResizeRows=false;_salesGrid.RowHeadersVisible=false;_salesGrid.AutoGenerateColumns=false;_salesGrid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_salesGrid.BackgroundColor=Color.White;_salesGrid.BorderStyle=BorderStyle.FixedSingle;_salesGrid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        _salesGrid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;_salesGrid.ColumnHeadersHeight=42;_salesGrid.RowTemplate.Height=44;_salesGrid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None;_salesGrid.ColumnHeadersDefaultCellStyle.WrapMode=DataGridViewTriState.True;_salesGrid.ColumnHeadersDefaultCellStyle.Padding=new Padding(3);_salesGrid.DefaultCellStyle.Padding=new Padding(3);
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Lot",HeaderText="Lot",DataPropertyName=nameof(LiveSaleHistoryItem.LotNumber),FillWeight=14});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Buyer",HeaderText="Buyer",DataPropertyName=nameof(LiveSaleHistoryItem.BuyerDisplay),FillWeight=42});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Quantity",HeaderText=IsWholesale?"Total Qty":"Qty",DataPropertyName=nameof(LiveSaleHistoryItem.Quantity),FillWeight=18});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Price",HeaderText="Price",DataPropertyName=nameof(LiveSaleHistoryItem.HammerPrice),DefaultCellStyle=new DataGridViewCellStyle{Format="C2"},FillWeight=22});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Total",HeaderText="Total",DataPropertyName=nameof(LiveSaleHistoryItem.Total),DefaultCellStyle=new DataGridViewCellStyle{Format="C2"},FillWeight=24});
        _salesGrid.SelectionChanged+=(_,_)=>ShowSelectedSaleDetails();_salesGrid.CellDoubleClick+=(_,e)=>{if(e.RowIndex>=0)EditSelectedSale();};panel.Controls.Add(_salesGrid,0,1);
        _historyDetails.Text="Select a sale to see its buyer and transaction details.";_historyDetails.Dock=DockStyle.Fill;_historyDetails.Padding=new Padding(8);_historyDetails.Font=new Font("Segoe UI",10F);_historyDetails.BackColor=Color.FromArgb(242,245,248);panel.Controls.Add(_historyDetails,0,2);
        ConfigureButton(_editSale,"EDIT SELECTED SALE",(_,_)=>EditSelectedSale(),true);_editSale.Enabled=false;_editSale.TabStop=false;panel.Controls.Add(_editSale,0,3);
        panel.RowCount = 5;
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var searchSold = new Button();
        ConfigureButton(searchSold, "SEARCH SOLD ITEMS", (_, _) => SearchSoldItems(), true);
        searchSold.TabStop = false;
        panel.Controls.Add(searchSold, 0, 4);
        return panel;
    }

    private void SearchSoldItems()
    {
        var viewModel = new SoldItemSearchViewModel(_service, _viewModel.SaleMode);
        using var form = new SoldItemSearchForm(viewModel);
        if (form.ShowDialog(this) != DialogResult.OK || viewModel.RecordedSale is not Sale sale) return;
        LoadLastSale();
        RefreshRecordedSales();
        RefreshBuyerCount();
        ShowStatus($"Added bidder #{sale.BidderNumber} to lot #{sale.LotNumber}: {sale.Quantity} at {sale.HammerPrice:C2} each.", true);
    }

    private Control BuildNotesAndQueuePanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.White, Padding = new Padding(14), Margin = new Padding(0, 12, 0, 0) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label { Text = "Auctioneer Notes", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DimGray }, 0, 0);
        panel.Controls.Add(new Label { Text = "Queued Next Lot", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DimGray }, 1, 0);

        _notes.Dock = DockStyle.Fill;
        _notes.Multiline = true;
        _notes.Font = new Font("Segoe UI", 11F);
        _notes.Leave += (_, _) => SaveCurrentLotNotes();
        panel.Controls.Add(_notes, 0, 1);

        ConfigureNumber(_queuedLot, 999999, 0);
        _queuedLot.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        panel.Controls.Add(_queuedLot, 1, 1);
        return panel;
    }

    private static void ConfigureButton(Button button, string text, EventHandler click, bool primary = false)
    {
        button.Text = text;
        button.AutoSize = false;
        button.Dock=DockStyle.Fill;
        button.Height=50;
        button.MinimumSize=new Size(120,48);
        button.Margin=new Padding(4);
        button.Font = new Font("Segoe UI", primary ? 11F : 9.5F, FontStyle.Bold);
        if (primary)
        {
            button.BackColor = Color.FromArgb(24, 96, 150);
            button.ForeColor = Color.White;
            button.FlatStyle = FlatStyle.Flat;
        }
        button.Click += click;
    }

    private static void ConfigureNumber(NumericUpDown control, decimal maximum, int decimalPlaces)
    {
        control.Minimum = 0;
        control.Maximum = maximum;
        control.DecimalPlaces = decimalPlaces;
        control.Dock = DockStyle.Fill;
        control.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        control.TextAlign = HorizontalAlignment.Center;
    }

    private static void SelectAllOnFocus(NumericUpDown control)=>control.Enter+=(_,_)=>control.Select(0,control.Text.Length);

    private static void AddEntryRow(TableLayoutPanel form, int row, string caption, Control control)
    {
        form.Controls.Add(new Label { Text = caption, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(24, 50, 74) }, 0, row);
        control.Margin = new Padding(6, 7, 6, 7);
        form.Controls.Add(control, 1, row);
    }

    private void HandleFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F2) { RepeatLastBuyer(); e.Handled = true; }
        else if (e.KeyCode == Keys.F3) { AdvanceToNextLot(); e.Handled = true; }
        else if (e.KeyCode == Keys.F4) { PassCurrentLot(); e.Handled = true; }
        else if (e.KeyCode == Keys.F8) { UndoLastSale(); e.Handled = true; }
        else if (e.KeyCode == Keys.F11) { ToggleFullScreen(); e.Handled = true; }
        else if (e.KeyCode == Keys.F12) { CloseCurrentLot(); e.Handled = true; }
        else if (e.KeyCode == Keys.Escape)
        {
            if (_fullScreen) ToggleFullScreen(); else Close();
            e.Handled = true;
        }
    }

    private void QuickAddLot()
    {
        if(_passOutMode.Checked&&!IsWholesale){ShowStatus("Close the current pass-out lot before creating another lot.",false);return;}
        if(_passOutMode.Checked&&IsWholesale)_passOutMode.Checked=false;
        try
        {
            var previousLot=Decimal.ToInt32(_lotNumber.Value);
            var lot=_viewModel.QuickAddLot(Decimal.ToInt32(_quickConsignorNumber.Value),_quickDescription.Text,previousLot);
            _lotNumber.Value=lot.LotNumber;
            _quickDescription.Clear();_passOutMode.Checked=false;_hammerPrice.Value=0;_bidderNumber.Value=0;_quantity.Value=1;_deals.Value=1;
            UpdateLotDisplay(true);
            ShowStatus(previousLot>0?$"Lot #{previousLot} closed. Lot #{lot.LotNumber} created and ready to sell.":$"Lot #{lot.LotNumber} created for Seller #{Decimal.ToInt32(_quickConsignorNumber.Value)} and ready to sell.",true);
            _hammerPrice.Focus();_hammerPrice.Select(0,_hammerPrice.Text.Length);
        }
        catch(Exception ex){ShowStatus(ex.Message,false);System.Media.SystemSounds.Hand.Play();}
    }

    private void PassCurrentLot()
    {
        if(_passOutMode.Checked){ShowStatus("Close the current pass-out lot before advancing.",false);return;}
        var lotNumber=Decimal.ToInt32(_lotNumber.Value);
        if(lotNumber<=0){ShowStatus("Enter or create a valid lot before passing it.",false);return;}
        if(MessageBox.Show($"Mark Lot #{lotNumber} as Passed and advance to the next item?","Pass Item",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        try
        {
            SaveCurrentLotNotes();
            _viewModel.PassLot(lotNumber);
            ShowStatus($"Lot #{lotNumber} passed.",true);
            _passOutMode.Checked=false; _bidderNumber.Value=0; _hammerPrice.Value=0;_quantity.Value=1;
            AdvanceToNextLot();
        }
        catch(Exception ex){ShowStatus(ex.Message,false);System.Media.SystemSounds.Hand.Play();}
    }

    private void CompleteSale(int? bidderOverride = null)
    {
        try
        {
            var lotNumber = Decimal.ToInt32(_lotNumber.Value);
            var bidderNumber = bidderOverride ?? Decimal.ToInt32(_bidderNumber.Value);
            var price = _hammerPrice.Value;
            var quantity=Decimal.ToInt32(_quantity.Value);
            var deals=IsWholesale&&_passOutMode.Checked?Decimal.ToInt32(_deals.Value):1;
            if (price >= 10000m && MessageBox.Show($"Verify the hammer price is {price:C2}.", "Verify Sale Price", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            var sale = _service.RecordSale(lotNumber, bidderNumber, price, _passOutMode.Checked,quantity,deals);
            _lastSale = sale;
            _lastBidderNumber = sale.BidderNumber;
            _previousSale.Text = sale.DisplayText;
            ShowStatus(IsWholesale
                ? $"RECORDED — Lot #{sale.LotNumber}, Bidder #{sale.BidderNumber}, {sale.QuantityPerDeal} per deal × {sale.Deals} deals = {sale.TotalQuantity} items, {sale.TotalPrice:C2}"
                : $"RECORDED — Lot #{sale.LotNumber}, Bidder #{sale.BidderNumber}, {sale.Quantity} × {sale.HammerPrice:C2} = {sale.TotalPrice:C2}", true);
            System.Media.SystemSounds.Asterisk.Play();
            _bidderNumber.Value = 0;
            if(!IsWholesale||!_passOutMode.Checked)_quantity.Value=1;
            _deals.Value=1;
            RefreshBuyerCount();
            RefreshRecordedSales();

            if (_passOutMode.Checked)
            {
                _bidderNumber.Focus();
                _bidderNumber.Select(0, _bidderNumber.Text.Length);
            }
            else
            {
                _hammerPrice.Value = 0;
                AdvanceToNextLot();
            }
        }
        catch (Exception ex)
        {
            ShowStatus(ex.Message, false);
            System.Media.SystemSounds.Hand.Play();
            MessageBox.Show(this,ex.Message,"Sale Not Recorded",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }

    private void RepeatLastBuyer()
    {
        if (!_passOutMode.Checked) { ShowStatus("F2 is available while Pass-Out Mode is active.", false); return; }
        if (_lastBidderNumber <= 0) { ShowStatus("No previous buyer is available to repeat.", false); return; }
        CompleteSale(_lastBidderNumber);
    }

    private void CloseCurrentLot()
    {
        var lotNumber = Decimal.ToInt32(_lotNumber.Value);
        if (lotNumber <= 0) { ShowStatus("Enter a valid lot number.", false); return; }
        if (MessageBox.Show($"Close Lot #{lotNumber} and move to the next lot?", "Close Lot", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        try
        {
            SaveCurrentLotNotes();
            _service.CloseLot(lotNumber);
            ShowStatus($"Lot #{lotNumber} closed.", true);
            System.Media.SystemSounds.Asterisk.Play();
            _passOutMode.Checked = false;
            _bidderNumber.Value = 0;
            _hammerPrice.Value = 0;
            _quantity.Value=1;
            _deals.Value=1;
            AdvanceToNextLot();
        }
        catch (Exception ex) { ShowStatus(ex.Message, false); System.Media.SystemSounds.Hand.Play(); }
    }

    private void UndoLastSale()
    {
        _lastSale = _service.GetLastSale();
        if (_lastSale is null) { ShowStatus("There is no buyer entry to undo.", false); return; }
        if (MessageBox.Show($"Undo {_lastSale.DisplayText}?", "Undo Last Buyer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        try
        {
            var undoneLot = _lastSale.LotNumber;
            _service.UndoLastSale(_lastSale.Id);
            ShowStatus($"Last buyer entry for Lot #{undoneLot} was undone.", true);
            _lotNumber.Value = undoneLot;
            _bidderNumber.Value = 0;
            LoadLastSale();
            UpdateLotDisplay(false);
            _bidderNumber.Focus();
        }
        catch (Exception ex) { ShowStatus(ex.Message, false); System.Media.SystemSounds.Hand.Play(); }
    }

    private void LoadLastSale()
    {
        _lastSale = _service.GetLastSale();
        _lastBidderNumber = _lastSale?.BidderNumber ?? 0;
        _previousSale.Text = _lastSale?.DisplayText ?? "No sales recorded yet.";
    }

    private void AdvanceToNextLot()
    {
        if(_passOutMode.Checked){ShowStatus("Close the current pass-out lot before advancing.",false);_bidderNumber.Focus();return;}
        _quickDescription.Clear();
        var queued = Decimal.ToInt32(_queuedLot.Value);
        if (queued > 0)
        {
            _queuedLot.Value = 0;
            _lotNumber.Value = queued;
            UpdateLotDisplay(true);
            _bidderNumber.Focus();
            return;
        }
        LoadNextAvailableLot(Decimal.ToInt32(_lotNumber.Value));
    }

    private void LoadNextAvailableLot(int afterLotNumber = 0)
    {
        var data = _service.Load();
        static bool IsOpen(AuctionLot lot)=>string.Equals(lot.Status,"Available",StringComparison.OrdinalIgnoreCase)||string.Equals(lot.Status,"Pass-Out",StringComparison.OrdinalIgnoreCase);
        var modeLots=data.Lots.Where(lot=>lot.SaleMode==_viewModel.SaleMode);
        var next = modeLots.Where(lot => lot.LotNumber > afterLotNumber && IsOpen(lot)).OrderBy(lot => lot.LotNumber).FirstOrDefault()
            ?? modeLots.Where(IsOpen).OrderBy(lot => lot.LotNumber).FirstOrDefault();

        if (next is null)
        {
            _lotNumber.Value = 0;
            _heroDescription.Text = "No available lots";
            ShowStatus("No available lots were found.", false);
            return;
        }
        _lotNumber.Value = next.LotNumber;
        UpdateLotDisplay(true);
        _bidderNumber.Focus();
        _bidderNumber.Select(0, _bidderNumber.Text.Length);
    }

    private void UpdateLotDisplay(bool loadNotes)
    {
        var number = Decimal.ToInt32(_lotNumber.Value);
        var lot = _service.Load().Lots.FirstOrDefault(item => item.LotNumber == number);
        if(lot is not null&&lot.SaleMode==_viewModel.SaleMode)_passOutMode.Checked=lot.PassOutMode;
        _heroLot.Text = number > 0 ? $"LOT {number}" : "LOT —";
        _heroDescription.Text = lot is null ? "Lot not found" : $"{lot.Description}   [{lot.Status}]";
        if (loadNotes) _notes.Text = number > 0 ? _service.GetSetting($"LotNotes:{number}", string.Empty) : string.Empty;
        RefreshBuyerCount();
        RefreshRecordedSales();
    }

    private void UpdateHeroPrice() => _heroPrice.Text = _hammerPrice.Value.ToString("C2");

    private void RefreshBuyerCount()
    {
        var lot = Decimal.ToInt32(_lotNumber.Value);
        var count = lot > 0 ? _service.GetSaleCountForLot(lot) : 0;
        _buyerCount.Text = $"{count}\nBUYER{(count == 1 ? string.Empty : "S")}";
    }

    private void RefreshRecordedSales()
    {
        _salesGrid.DataSource=null;
        _salesGrid.DataSource=_viewModel.GetSaleHistory().ToList();
        _salesGrid.ClearSelection();
        if(_salesGrid.Rows.Count>0)_salesGrid.FirstDisplayedScrollingRowIndex=_salesGrid.Rows.Count-1;
        ShowSelectedSaleDetails();
    }

    private void ShowSelectedSaleDetails()
    {
        if(_salesGrid.CurrentRow?.DataBoundItem is not LiveSaleHistoryItem sale)
        {
            _historyDetails.Text="Select a sale to see its buyer and transaction details.";_editSale.Enabled=false;return;
        }
        var quantity=IsWholesale?$"{sale.QuantityPerDeal} per deal × {sale.Deals} deals = {sale.Quantity} items":$"Quantity {sale.Quantity}";
        _historyDetails.Text=$"Lot #{sale.LotNumber}: {sale.Description}\r\n{sale.BuyerDisplay}\r\n{quantity} × {sale.HammerPrice:C2} = {sale.Total:C2}   •   {sale.LocalTime}";
        _editSale.Enabled=true;
    }

    private void EditSelectedSale()
    {
        if(_salesGrid.CurrentRow?.DataBoundItem is not LiveSaleHistoryItem sale){ShowStatus("Select a sale from Live Sale History.",false);return;}
        using var form=new LiveSaleCorrectionForm(sale);
        if(form.ShowDialog(this)!=DialogResult.OK||form.Request is null)return;
        if(MessageBox.Show(this,$"Save this correction for Lot #{sale.LotNumber}? The original transaction remains in the audit history.","Confirm Sale Correction",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        try
        {
            _viewModel.CorrectSale(sale.BidderNumber,form.Request);LoadLastSale();RefreshRecordedSales();RefreshBuyerCount();
            ShowStatus($"Sale for Lot #{sale.LotNumber} was corrected and invoice totals were refreshed.",true);System.Media.SystemSounds.Asterisk.Play();
        }
        catch(Exception exception){ShowStatus(exception.Message,false);System.Media.SystemSounds.Hand.Play();MessageBox.Show(this,exception.Message,"Sale Not Corrected",MessageBoxButtons.OK,MessageBoxIcon.Warning);}
    }

    private void SaveCurrentLotNotes()
    {
        var lot = Decimal.ToInt32(_lotNumber.Value);
        if (lot <= 0) return;
        _service.SaveSettings(new Dictionary<string, string> { [$"LotNotes:{lot}"] = _notes.Text.Trim() });
    }

    private void UpdateModeDisplay()
    {
        var entryState=_viewModel.GetEntryState(_passOutMode.Checked);
        _quickConsignorNumber.Enabled=entryState.CanChangeSeller;
        _quickDescription.ReadOnly=!entryState.CanChangeDescription;
        _createLot.Enabled=entryState.CanChangeDescription;
        _lotNumber.Enabled=entryState.CanChangeLot;
        _hammerPrice.Enabled=entryState.CanChangeHammerPrice;
        _bidderNumber.Enabled=entryState.CanChangeBidder;
        _quantity.Enabled=entryState.CanChangeQuantity;
        _deals.Enabled=entryState.CanChangeDeals;
        _passLot.Enabled=!_passOutMode.Checked;
        if (_passOutMode.Checked)
        {
            _modeLabel.Text = IsWholesale ? "WHOLESALE PASS-OUT — price and quantity per deal stay locked" : "PASS-OUT MODE — lot and price stay open for additional buyers";
            _modeLabel.BackColor = Color.FromArgb(255, 235, 180);
            _modeLabel.ForeColor = Color.FromArgb(105, 65, 0);
            _completeSale.Text = "RECORD BUYER  (Enter)";
            if(IsHandleCreated)FocusBidderEntry();else _focusBidderWhenShown=true;
        }
        else
        {
            _modeLabel.Text = "STANDARD LOT — one buyer";
            _modeLabel.BackColor = Color.FromArgb(225, 230, 235);
            _modeLabel.ForeColor = Color.FromArgb(45, 55, 65);
            _completeSale.Text = "COMPLETE SALE  (Enter)";
        }
    }

    private void FocusBidderEntry()
    {
        _bidderNumber.Focus();
        _bidderNumber.Select(0,_bidderNumber.Text.Length);
    }

    private void ToggleFullScreen()
    {
        if (!_fullScreen)
        {
            _savedBorderStyle = FormBorderStyle;
            _savedWindowState = WindowState;
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            _fullScreen = true;
            _fullScreenButton.Text = "Exit Fullscreen  (F11)";
        }
        else
        {
            TopMost = false;
            FormBorderStyle = _savedBorderStyle;
            WindowState = _savedWindowState;
            _fullScreen = false;
            _fullScreenButton.Text = "Fullscreen  (F11)";
        }
    }

    private void ShowStatus(string message, bool success)
    {
        _status.Text = message;
        _status.BackColor = success ? Color.FromArgb(210, 239, 218) : Color.FromArgb(249, 215, 218);
        _status.ForeColor = success ? Color.FromArgb(20, 90, 45) : Color.FromArgb(140, 25, 35);
    }
}
