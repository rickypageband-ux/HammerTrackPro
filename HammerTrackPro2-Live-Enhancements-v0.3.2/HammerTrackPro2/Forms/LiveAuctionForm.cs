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
    private readonly NumericUpDown _queuedLot = new();
    private readonly CheckBox _passOutMode = new();
    private readonly Label _heroLot = new();
    private readonly Label _heroDescription = new();
    private readonly Label _heroPrice = new();
    private readonly Label _buyerCount = new();
    private readonly Label _status = new();
    private readonly Label _previousSale = new();
    private readonly DataGridView _salesGrid = new();
    private readonly Label _modeLabel = new();
    private readonly TextBox _notes = new();
    private readonly Button _completeSale = new();
    private readonly Button _closeLot = new();
    private readonly Button _passLot = new();
    private readonly Button _undoLastSale = new();
    private readonly Button _fullScreenButton = new();
    private Sale? _lastSale;
    private int _lastBidderNumber;
    private bool _fullScreen;
    private FormBorderStyle _savedBorderStyle;
    private FormWindowState _savedWindowState;

    public LiveAuctionForm(IAuctionService service,LiveAuctionViewModel viewModel)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _viewModel=viewModel??throw new ArgumentNullException(nameof(viewModel));
        Text = "HammerTrack Pro - Live Auction";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1000, 760);
        Width = 1180;
        Height = 860;
        Font = new Font("Segoe UI", 12F);
        KeyPreview = true;
        KeyDown += HandleFormKeyDown;
        Controls.Add(BuildLayout());
        AcceptButton = _completeSale;
        LoadLastSale();
        LoadNextAvailableLot();
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(16),
            BackColor = Color.FromArgb(242, 245, 248)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
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
        root.Controls.Add(BuildRecordedSalesPanel(), 0, 5);
        root.Controls.Add(BuildNotesAndQueuePanel(), 0, 6);

        var help = new Label
        {
            Text = "Enter: record buyer    F2: same buyer    F3: next lot    F4: pass item    F8: undo    F11: fullscreen    F12: close lot    Esc: exit fullscreen/close",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 10, 0, 0)
        };
        root.Controls.Add(help, 0, 7);
        return root;
    }

    private Control BuildQuickAddPanel()
    {
        var panel=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=5,Padding=new Padding(12),Margin=new Padding(0,8,0,0),BackColor=Color.White };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,130));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text="Quick Add Seller Number",AutoSize=true,Anchor=AnchorStyles.Left,Font=new Font("Segoe UI",11F,FontStyle.Bold) },0,0);
        _quickConsignorNumber.Minimum=0; _quickConsignorNumber.Maximum=999999; _quickConsignorNumber.Dock=DockStyle.Fill;
        panel.Controls.Add(_quickConsignorNumber,1,0);
        panel.Controls.Add(new Label { Text="Lot Description / Name",AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(16,3,3,3),Font=new Font("Segoe UI",11F,FontStyle.Bold) },2,0);
        _quickDescription.Dock=DockStyle.Fill;
        _quickDescription.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){QuickAddLot();e.SuppressKeyPress=true;}};
        panel.Controls.Add(_quickDescription,3,0);
        var add=new Button(); ConfigureButton(add,"CREATE LOT",(_,_)=>QuickAddLot(),true);add.MinimumSize=new Size(150,48);add.Width=150;panel.Controls.Add(add,4,0);
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
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18), Margin = new Padding(0, 12, 0, 12),MinimumSize=new Size(0,400) };
        var form = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));

        ConfigureNumber(_lotNumber, 999999, 0);
        ConfigureNumber(_bidderNumber, 999999, 0);
        ConfigureNumber(_hammerPrice, 100000000, 2);
        ConfigureNumber(_quantity, 1000000, 0);_quantity.Minimum=1;_quantity.Value=1;
        _hammerPrice.ThousandsSeparator = true;

        _lotNumber.ValueChanged += (_, _) => UpdateLotDisplay(true);
        _lotNumber.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { _hammerPrice.Focus(); _hammerPrice.Select(0, _hammerPrice.Text.Length); e.SuppressKeyPress = true; } };
        _hammerPrice.ValueChanged += (_, _) => UpdateHeroPrice();
        _hammerPrice.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { _bidderNumber.Focus();_bidderNumber.Select(0,_bidderNumber.Text.Length);e.SuppressKeyPress=true; } };
        _bidderNumber.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            _quantity.Focus();_quantity.Select(0,_quantity.Text.Length);
            e.SuppressKeyPress = true;
        };
        _quantity.KeyDown+=(_,e)=>{if(e.KeyCode==Keys.Enter){CompleteSale();e.SuppressKeyPress=true;}};

        _passOutMode.Text = "PASS-OUT MODE — keep this lot and price open for more buyers";
        _passOutMode.AutoSize = true;
        _passOutMode.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        _passOutMode.CheckedChanged += (_, _) => UpdateModeDisplay();

        AddEntryRow(form, 0, "Lot Number", _lotNumber);
        AddEntryRow(form, 1, "Hammer Price (each)", _hammerPrice);
        AddEntryRow(form, 2, "Bidder Number", _bidderNumber);
        AddEntryRow(form, 3, "Quantity", _quantity);
        form.Controls.Add(new Label(), 0, 4);
        form.Controls.Add(_passOutMode, 1, 4);

        var buttons = new TableLayoutPanel { Dock=DockStyle.Top,Height=58,MaximumSize=new Size(0,58),ColumnCount=5,RowCount=1,Margin=new Padding(0,4,0,0) };
        for(var column=0;column<5;column++)buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));
        ConfigureButton(_completeSale, "RECORD BUYER  (Enter)", (_, _) => CompleteSale(), true);
        ConfigureButton(_closeLot, "Close Lot  (F12)", (_, _) => CloseCurrentLot());
        ConfigureButton(_passLot, "Pass Item / Next  (F4)", (_, _) => PassCurrentLot());
        ConfigureButton(_undoLastSale, "Undo Last Buyer  (F8)", (_, _) => UndoLastSale());
        ConfigureButton(_fullScreenButton, "Fullscreen  (F11)", (_, _) => ToggleFullScreen());
        buttons.Controls.Add(_completeSale,0,0);buttons.Controls.Add(_passLot,1,0);buttons.Controls.Add(_closeLot,2,0);buttons.Controls.Add(_undoLastSale,3,0);buttons.Controls.Add(_fullScreenButton,4,0);
        form.SetColumnSpan(buttons,2);form.Controls.Add(buttons,0,5);
        card.Controls.Add(form);
        return card;
    }

    private Control BuildRecordedSalesPanel()
    {
        var panel=new TableLayoutPanel{Dock=DockStyle.Fill,BackColor=Color.White,Padding=new Padding(14),RowCount=2,ColumnCount=1};panel.RowStyles.Add(new RowStyle(SizeType.Absolute,34));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var caption=new Label{Text="RECORDED SALES FOR CURRENT LOT — verify every bidder before moving on",Dock=DockStyle.Fill,Font=new Font("Segoe UI",11F,FontStyle.Bold),ForeColor=Color.FromArgb(24,50,74),TextAlign=ContentAlignment.MiddleLeft};panel.Controls.Add(caption,0,0);
        _salesGrid.Dock=DockStyle.Fill;_salesGrid.ReadOnly=true;_salesGrid.AllowUserToAddRows=false;_salesGrid.AllowUserToDeleteRows=false;_salesGrid.AllowUserToResizeRows=false;_salesGrid.RowHeadersVisible=false;_salesGrid.AutoGenerateColumns=false;_salesGrid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;_salesGrid.BackgroundColor=Color.White;_salesGrid.BorderStyle=BorderStyle.FixedSingle;_salesGrid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Lot",HeaderText="Lot",DataPropertyName=nameof(Sale.LotNumber),FillWeight=16});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Bidder",HeaderText="Bidder",DataPropertyName=nameof(Sale.BidderNumber),FillWeight=20});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Quantity",HeaderText="Quantity",DataPropertyName=nameof(Sale.Quantity),FillWeight=18});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Price",HeaderText="Price Each",DataPropertyName=nameof(Sale.HammerPrice),DefaultCellStyle=new DataGridViewCellStyle{Format="C2"},FillWeight=23});
        _salesGrid.Columns.Add(new DataGridViewTextBoxColumn{Name="Total",HeaderText="Total",DataPropertyName=nameof(Sale.TotalPrice),DefaultCellStyle=new DataGridViewCellStyle{Format="C2"},FillWeight=23});
        panel.Controls.Add(_salesGrid,0,1);
        return panel;
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
        try
        {
            var previousLot=Decimal.ToInt32(_lotNumber.Value);
            var lot=_viewModel.QuickAddLot(Decimal.ToInt32(_quickConsignorNumber.Value),_quickDescription.Text,previousLot);
            _lotNumber.Value=lot.LotNumber;
            _quickDescription.Clear();_passOutMode.Checked=false;_hammerPrice.Value=0;_bidderNumber.Value=0;_quantity.Value=1;
            UpdateLotDisplay(true);
            ShowStatus(previousLot>0?$"Lot #{previousLot} closed. Lot #{lot.LotNumber} created and ready to sell.":$"Lot #{lot.LotNumber} created for Seller #{Decimal.ToInt32(_quickConsignorNumber.Value)} and ready to sell.",true);
            _hammerPrice.Focus();_hammerPrice.Select(0,_hammerPrice.Text.Length);
        }
        catch(Exception ex){ShowStatus(ex.Message,false);System.Media.SystemSounds.Hand.Play();}
    }

    private void PassCurrentLot()
    {
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
            if (price >= 10000m && MessageBox.Show($"Verify the hammer price is {price:C2}.", "Verify Sale Price", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

            var sale = _service.RecordSale(lotNumber, bidderNumber, price, _passOutMode.Checked,quantity);
            _lastSale = sale;
            _lastBidderNumber = sale.BidderNumber;
            _previousSale.Text = sale.DisplayText;
            ShowStatus($"RECORDED — Lot #{sale.LotNumber}, Bidder #{sale.BidderNumber}, {sale.Quantity} × {sale.HammerPrice:C2} = {sale.TotalPrice:C2}", true);
            System.Media.SystemSounds.Asterisk.Play();
            _bidderNumber.Value = 0;
            _quantity.Value=1;
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
        var next = data.Lots.Where(lot => lot.LotNumber > afterLotNumber && IsOpen(lot)).OrderBy(lot => lot.LotNumber).FirstOrDefault()
            ?? data.Lots.Where(IsOpen).OrderBy(lot => lot.LotNumber).FirstOrDefault();

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
        var lot=Decimal.ToInt32(_lotNumber.Value);
        _salesGrid.DataSource=null;
        _salesGrid.DataSource=lot>0?_service.GetSalesForLot(lot).ToList():[];
        _salesGrid.ClearSelection();
        if(_salesGrid.Rows.Count>0)_salesGrid.FirstDisplayedScrollingRowIndex=_salesGrid.Rows.Count-1;
    }

    private void SaveCurrentLotNotes()
    {
        var lot = Decimal.ToInt32(_lotNumber.Value);
        if (lot <= 0) return;
        _service.SaveSettings(new Dictionary<string, string> { [$"LotNotes:{lot}"] = _notes.Text.Trim() });
    }

    private void UpdateModeDisplay()
    {
        if (_passOutMode.Checked)
        {
            _modeLabel.Text = "PASS-OUT MODE — lot and price stay open for additional buyers";
            _modeLabel.BackColor = Color.FromArgb(255, 235, 180);
            _modeLabel.ForeColor = Color.FromArgb(105, 65, 0);
            _completeSale.Text = "RECORD BUYER  (Enter)";
        }
        else
        {
            _modeLabel.Text = "STANDARD LOT — one buyer";
            _modeLabel.BackColor = Color.FromArgb(225, 230, 235);
            _modeLabel.ForeColor = Color.FromArgb(45, 55, 65);
            _completeSale.Text = "COMPLETE SALE  (Enter)";
        }
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
