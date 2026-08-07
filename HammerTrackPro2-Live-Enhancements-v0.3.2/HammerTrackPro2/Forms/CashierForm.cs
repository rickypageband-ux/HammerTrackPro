using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class CashierForm : Form
{
    private static readonly Color Navy=Color.FromArgb(24,50,74);
    private static readonly Color Blue=Color.FromArgb(24,96,150);
    private static readonly Color Gold=Color.FromArgb(224,168,54);
    private static readonly Color Canvas=Color.FromArgb(242,245,248);
    private readonly CashierViewModel _viewModel;
    private readonly ICashierService _cashierService;
    private readonly Label _connection=new() { AutoSize=true,Text="Checking host connection..." };
    private readonly System.Windows.Forms.Timer _connectionTimer=new() { Interval=5000 };
    private readonly WorkstationConfiguration? _configuration;
    private bool _checkingConnection;
    private readonly NumericUpDown _bidderNumber=new() { Minimum=1,Maximum=999999 };
    private readonly Label _heading=new() { AutoSize=true };
    private readonly Label _totals=new() { AutoSize=true };
    private readonly DataGridView _purchases=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly DataGridView _payments=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly DataGridView _pending=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly DataGridView _refunds=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly ComboBox _method=new() { DropDownStyle=ComboBoxStyle.DropDownList,DataSource=Enum.GetValues<PaymentMethod>() };
    private readonly NumericUpDown _amount=new() { DecimalPlaces=2,Maximum=100000000 };
    private readonly NumericUpDown _cashTendered=new() { DecimalPlaces=2,Maximum=100000000 };
    private readonly TextBox _changeDue=new() { ReadOnly=true,Text="$0.00",Width=110,BackColor=Color.White };
    private readonly TextBox _reference=new();
    private readonly Label _paymentPreview=new(){AutoSize=true,ForeColor=Color.DarkGreen,Font=new Font("Segoe UI",9F,FontStyle.Bold)};
    private readonly NumericUpDown _consignorNumber=new() { Minimum=1,Maximum=999999 };
    private readonly Label _settlementHeading=new() { AutoSize=true };
    private readonly DataGridView _settlementLots=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly DataGridView _payouts=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly ComboBox _payoutMethod=new() { DropDownStyle=ComboBoxStyle.DropDownList,DataSource=Enum.GetValues<ConsignorPayoutMethod>() };
    private readonly NumericUpDown _payoutAmount=new() { DecimalPlaces=2,Maximum=100000000 };
    private readonly TextBox _payoutReference=new() { Width=160 };
    private readonly Button _fullScreenButton=new(){Text="Full Screen"};
    private bool _fullScreen;

    public CashierForm(CashierViewModel viewModel,ICashierService cashierService,WorkstationConfiguration? configuration=null)
    {
        _viewModel=viewModel; _cashierService=cashierService; _configuration=configuration; Text="HammerTrack Pro — Cashier"; Width=1280; Height=820;MinimumSize=new Size(1100,720);StartPosition=FormStartPosition.CenterScreen;BackColor=Canvas;KeyPreview=true;
        var header=BuildHeader();
        var lookup=ActionFlow();
        lookup.Controls.Add(new Label { Text="Auction Bidder Number",AutoSize=true }); lookup.Controls.Add(_bidderNumber);
        lookup.Controls.Add(Button("Lookup Buyer",Lookup)); lookup.Controls.Add(Button("Add Buyer",RegisterBuyer)); lookup.Controls.Add(Button("Add Seller",RegisterConsignor)); lookup.Controls.Add(Button("Bidder Cards",ShowBidderCards)); lookup.Controls.Add(Button("Correct Sale",CorrectSale)); lookup.Controls.Add(Button("Refund",RecordRefund)); lookup.Controls.Add(Button("Unpaid Buyers",ShowUnpaid)); lookup.Controls.Add(Button("End-of-Night",()=>{using var f=new AuctionSettlementForm(_cashierService);f.ShowDialog(this);})); lookup.Controls.Add(Button("Print Invoice",PrintInvoice));
        if(configuration is not null) lookup.Controls.Add(Button("Network Setup",OpenNetworkSetup));
        var paymentEntry=ActionFlow();
        paymentEntry.Controls.Add(new Label { Text="Method",AutoSize=true }); paymentEntry.Controls.Add(_method);
        paymentEntry.Controls.Add(new Label { Text="Invoice Amount (before card fee)",AutoSize=true }); paymentEntry.Controls.Add(_amount);
        paymentEntry.Controls.Add(new Label { Text="Cash Tendered",AutoSize=true }); paymentEntry.Controls.Add(_cashTendered);
        paymentEntry.Controls.Add(new Label { Text="Change Due",AutoSize=true }); paymentEntry.Controls.Add(_changeDue);
        paymentEntry.Controls.Add(new Label { Text="Check/Card Reference",AutoSize=true }); paymentEntry.Controls.Add(_reference);
        paymentEntry.Controls.Add(_paymentPreview);
        paymentEntry.Controls.Add(Button("Add Payment",AddPayment)); paymentEntry.Controls.Add(Button("Remove Selected",RemovePayment));
        paymentEntry.Controls.Add(Button("Record Payments",RecordPayments));
        var tabs=new TabControl { Dock=DockStyle.Fill,Font=new Font("Segoe UI",10F,FontStyle.Bold),Padding=new Point(18,8) };
        tabs.TabPages.Add(Page("Purchases",_purchases)); tabs.TabPages.Add(Page("Payment History",_payments)); tabs.TabPages.Add(Page("Pending Split Payments",_pending));
        tabs.TabPages.Add(Page("Refund History",_refunds));
        tabs.TabPages.Add(Page("Seller Lots",_settlementLots)); tabs.TabPages.Add(Page("Seller Payouts",_payouts));
        var sellerEntry=ActionFlow();
        sellerEntry.Controls.Add(new Label { Text="Seller Number",AutoSize=true }); sellerEntry.Controls.Add(_consignorNumber); sellerEntry.Controls.Add(Button("Lookup Seller",LookupConsignor)); sellerEntry.Controls.Add(Button("Add Seller",RegisterConsignor)); sellerEntry.Controls.Add(Button("Print Settlement",PrintSettlement));sellerEntry.Controls.Add(Button("Print Item Summary",PrintSellerItemSummary));
        sellerEntry.Controls.Add(new Label { Text="Payout Method",AutoSize=true }); sellerEntry.Controls.Add(_payoutMethod); sellerEntry.Controls.Add(new Label { Text="Amount",AutoSize=true }); sellerEntry.Controls.Add(_payoutAmount);
        sellerEntry.Controls.Add(new Label { Text="Check/ACH Reference",AutoSize=true }); sellerEntry.Controls.Add(_payoutReference); sellerEntry.Controls.Add(Button("Record Seller Payout",RecordConsignorPayout));
        var summary=new Panel{Dock=DockStyle.Fill,Height=70,Padding=new Padding(18,8,18,8),BackColor=Color.White};_heading.Dock=DockStyle.Top;_heading.Font=new Font("Segoe UI",13F,FontStyle.Bold);_heading.ForeColor=Navy;_totals.Dock=DockStyle.Bottom;_totals.Font=new Font("Segoe UI",10F,FontStyle.Bold);summary.Controls.Add(_totals);summary.Controls.Add(_heading);
        var sellerBand=new TableLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,RowCount=2,ColumnCount=1,BackColor=Color.White,Padding=new Padding(12,6,12,8)};sellerBand.Controls.Add(_settlementHeading,0,0);sellerBand.Controls.Add(sellerEntry,0,1);_settlementHeading.Font=new Font("Segoe UI",10F,FontStyle.Bold);_settlementHeading.ForeColor=Navy;
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=6,ColumnCount=1,Padding=new Padding(12,0,12,12),BackColor=Canvas };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,78));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,72));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(header,0,0);layout.Controls.Add(Band("BUYER & INVOICE",lookup),0,1);layout.Controls.Add(summary,0,2);layout.Controls.Add(tabs,0,3);layout.Controls.Add(Band("PAYMENT",paymentEntry),0,4);layout.Controls.Add(sellerBand,0,5);
        Controls.Add(layout);
        StyleInputs();StyleGrid(_purchases);StyleGrid(_payments);StyleGrid(_pending);StyleGrid(_refunds);StyleGrid(_settlementLots);StyleGrid(_payouts);
        _method.SelectedIndexChanged+=(_,_)=>PaymentMethodChanged();_amount.ValueChanged+=(_,_)=>{RefreshPaymentPreview();RefreshCashChange();};_cashTendered.ValueChanged+=(_,_)=>RefreshCashChange();
        _fullScreenButton.Click+=(_,_)=>ToggleFullScreen();KeyDown+=(_,e)=>{if(e.KeyCode==Keys.F11){ToggleFullScreen();e.Handled=true;}else if(e.KeyCode==Keys.Escape&&_fullScreen){ToggleFullScreen();e.Handled=true;}};
        PrepareSelectedPaymentAmount();RefreshPaymentPreview();
        Shown+=async(_,_)=>await CheckConnection(); _connectionTimer.Tick+=async(_,_)=>await CheckConnection(); _connectionTimer.Start();
        FormClosed+=(_,_)=>{_connectionTimer.Stop();_connectionTimer.Dispose();};
    }

    private Control BuildHeader(){var panel=new Panel{Dock=DockStyle.Fill,BackColor=Navy,Padding=new Padding(20,10,16,10)};var brand=new Label{Text="HAMMERTRACK PRO",Dock=DockStyle.Left,Width=290,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",20F,FontStyle.Bold),ForeColor=Color.White};var subtitle=new Label{Text="AUCTION CASHIER",Dock=DockStyle.Left,Width=210,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",11F,FontStyle.Bold),ForeColor=Gold};StyleButton(_fullScreenButton,true);_fullScreenButton.Dock=DockStyle.Right;_connection.Dock=DockStyle.Right;_connection.Width=220;_connection.TextAlign=ContentAlignment.MiddleCenter;_connection.Font=new Font("Segoe UI",10F,FontStyle.Bold);panel.Controls.Add(_fullScreenButton);panel.Controls.Add(_connection);panel.Controls.Add(subtitle);panel.Controls.Add(brand);return panel;}
    private static FlowLayoutPanel ActionFlow()=>new(){Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,Padding=new Padding(6),BackColor=Color.White};
    private static Control Band(string title,Control content){var panel=new Panel{Dock=DockStyle.Fill,AutoSize=true,BackColor=Color.White,Padding=new Padding(6,4,6,4),Margin=new Padding(0,8,0,0)};panel.Controls.Add(content);panel.Controls.Add(new Label{Text=title,Dock=DockStyle.Left,Width=120,TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",10F,FontStyle.Bold),ForeColor=Navy});return panel;}
    private static TabPage Page(string name,Control content) { var page=new TabPage(name){BackColor=Color.White,Padding=new Padding(6)}; page.Controls.Add(content); return page; }
    private static Button Button(string text,Action action) { var button=new Button { Text=text };StyleButton(button);button.Click+=(_,_)=>Execute(action); return button; }
    private static void StyleButton(Button button,bool accent=false){button.AutoSize=false;button.Width=142;button.Height=42;button.Margin=new Padding(5);button.FlatStyle=FlatStyle.Flat;button.FlatAppearance.BorderSize=1;button.FlatAppearance.BorderColor=accent?Gold:Color.FromArgb(15,72,112);button.BackColor=accent?Gold:Blue;button.ForeColor=accent?Navy:Color.White;button.Font=new Font("Segoe UI",10F,FontStyle.Bold);button.Cursor=Cursors.Hand;}
    private void StyleInputs(){foreach(var control in new Control[]{_bidderNumber,_method,_amount,_cashTendered,_changeDue,_reference,_consignorNumber,_payoutMethod,_payoutAmount,_payoutReference}){control.Font=new Font("Segoe UI",11F);control.Height=34;control.Margin=new Padding(5,9,10,5);}foreach(var label in new[]{_paymentPreview})label.Margin=new Padding(8,13,8,5);}
    private static void StyleGrid(DataGridView grid){grid.BackgroundColor=Color.White;grid.BorderStyle=BorderStyle.None;grid.RowHeadersVisible=false;grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.ColumnHeadersHeight=38;grid.RowTemplate.Height=32;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersDefaultCellStyle.BackColor=Navy;grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI",10F,FontStyle.Bold);grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(214,232,245);grid.DefaultCellStyle.SelectionForeColor=Navy;grid.AlternatingRowsDefaultCellStyle.BackColor=Color.FromArgb(246,249,251);}
    private void ToggleFullScreen(){_fullScreen=!_fullScreen;FormBorderStyle=_fullScreen?FormBorderStyle.None:FormBorderStyle.Sizable;WindowState=_fullScreen?FormWindowState.Maximized:FormWindowState.Normal;_fullScreenButton.Text=_fullScreen?"Exit Full Screen":"Full Screen";}
    private void Lookup() { _viewModel.Lookup(Decimal.ToInt32(_bidderNumber.Value)); RefreshInvoice(); }
    private void AddPayment() { var method=(PaymentMethod)_method.SelectedItem!;var paymentAmount=method==PaymentMethod.Cash?_cashTendered.Value:_amount.Value;if(method==PaymentMethod.Cash&&paymentAmount<=0)throw new InvalidOperationException("Enter the cash received in Cash Tendered.");_viewModel.AddPayment(method,paymentAmount,_reference.Text); _amount.Value=0;_cashTendered.Value=0; _reference.Clear(); RefreshInvoice(); }
    private void RemovePayment() { if (_pending.CurrentRow?.DataBoundItem is PaymentRequest payment) _viewModel.RemovePayment(payment); RefreshInvoice(); }
    private void RecordPayments() { _viewModel.RecordPayments(); RefreshInvoice(); }
    private void LookupConsignor() { _viewModel.LookupConsignor(Decimal.ToInt32(_consignorNumber.Value)); RefreshSettlement(); }
    private void RegisterBuyer(){using var f=new CashierRegistrationForm(new CashierRegistrationViewModel(false,_cashierService.GetRegisteredBidders()));if(f.ShowDialog(this)!=DialogResult.OK||f.Buyer is null)return;_viewModel.RegisterBuyer(f.Buyer);_bidderNumber.Value=f.Buyer.BidderNumber;RefreshInvoice();}
    private void RegisterConsignor(){using var f=new CashierRegistrationForm(new CashierRegistrationViewModel(true,_cashierService.GetRegisteredBidders()));if(f.ShowDialog(this)!=DialogResult.OK||f.Consignor is null)return;_viewModel.RegisterConsignor(f.Consignor);_consignorNumber.Value=f.Consignor.ConsignorNumber;RefreshSettlement();}
    private void ShowBidderCards(){using var f=new BidderCardForm(new BidderCardViewModel(_cashierService));f.ShowDialog(this);}
    private void RecordRefund(){var invoice=_viewModel.Invoice??throw new InvalidOperationException("Look up a buyer before recording a refund.");using var form=new RefundForm(invoice);if(form.ShowDialog(this)!=DialogResult.OK||form.Request is null)return;if(MessageBox.Show(this,$"Record a {form.Request.Amount:C2} refund? Card fees will not be refunded.","Confirm Refund",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;_viewModel.RecordRefund(form.Request);RefreshInvoice();}
    private void CorrectSale(){var invoice=_viewModel.Invoice??throw new InvalidOperationException("Look up a buyer before correcting a sale.");if(_purchases.CurrentRow?.DataBoundItem is not InvoiceLine line)throw new InvalidOperationException("Select the customer sale to correct in the Purchases tab.");using var form=new SaleCorrectionForm(line,invoice.BidderNumber);if(form.ShowDialog(this)!=DialogResult.OK||form.Request is null)return;if(MessageBox.Show(this,"Save this audited sale correction? The original sale will remain in history.","Confirm Sale Correction",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;_viewModel.CorrectSale(form.Request);RefreshInvoice();}
    private void ShowUnpaid(){var rows=_cashierService.GetUnpaidInvoices();using var f=new Form{Text="Current Auction - Unpaid Buyers",StartPosition=FormStartPosition.CenterParent,Width=800,Height=500};var grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,DataSource=rows.ToList(),AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill};f.Controls.Add(grid);if(rows.Count==0)f.Controls.Add(new Label{Text="Everyone in the current auction is paid in full.",Dock=DockStyle.Top,Height=44,TextAlign=ContentAlignment.MiddleCenter,BackColor=Color.Honeydew,ForeColor=Color.DarkGreen});f.ShowDialog(this);}
    private void RecordConsignorPayout()
    {
        var settlement=_viewModel.Settlement??throw new InvalidOperationException("Look up a seller before recording a payout.");
        if(!string.Equals(_cashierService.GetCurrentAuction().Status,"Closed",StringComparison.OrdinalIgnoreCase)
           && MessageBox.Show("This auction is still open. The seller's balance may change as more lots sell or buyer purchases are added. Record this early payout now?","Early Seller Payout",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        if(MessageBox.Show($"Record a {_payoutMethod.SelectedItem} payout of {_payoutAmount.Value:C2} to {settlement.ConsignorName}?","Confirm Seller Payout",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        _viewModel.RecordConsignorPayout((ConsignorPayoutMethod)_payoutMethod.SelectedItem!,_payoutAmount.Value,_payoutReference.Text); _payoutAmount.Value=0; _payoutReference.Clear(); RefreshSettlement();
    }
    private void PrintInvoice()
    {
        if(_method.SelectedItem is not PaymentMethod method)throw new InvalidOperationException("Select a payment method.");
        var invoice=_viewModel.CreateInvoicePreview(method,_amount.Value);
        using var document=new InvoicePrinter().CreateDocument(invoice,_cashierService.GetCurrentAuction());
        ShowPrintPreview(document);
    }
    private void PrintSettlement()
    {
        var settlement=_viewModel.Settlement??throw new InvalidOperationException("Look up a seller before printing a settlement.");
        using var document=new SettlementPrinter().CreateDocument(settlement,_cashierService.GetCurrentAuction());
        ShowPrintPreview(document);
    }
    private void PrintSellerItemSummary(){var settlement=_viewModel.Settlement??throw new InvalidOperationException("Look up a seller before printing an item summary.");using var document=new SellerItemSummaryPrinter().CreateDocument(settlement,_cashierService.GetCurrentAuction());ShowPrintPreview(document);}
    private void ShowPrintPreview(System.Drawing.Printing.PrintDocument document)
    {
        using var preview=new PrintPreviewDialog
        {
            Document=document,
            Text=$"Print Preview - {document.DocumentName}",
            Width=1100,
            Height=800,
            StartPosition=FormStartPosition.CenterParent,
            UseAntiAlias=true
        };
        preview.ShowDialog(this);
    }
    private async Task CheckConnection()
    {
        if(_checkingConnection)return; _checkingConnection=true;
        try
        {
            var connected=await _cashierService.CheckConnectionAsync();
            _connection.Text=connected?"Connected to auction host":"Host disconnected — retrying";
            _connection.ForeColor=connected?Color.DarkGreen:Color.DarkRed;
        }
        finally {_checkingConnection=false;}
    }
    private void OpenNetworkSetup()
    {
        if(_configuration is null)return;
        using var setup=new WorkstationSetupForm(_configuration);
        if(setup.ShowDialog(this)==DialogResult.OK) MessageBox.Show("Restart HammerTrack to apply the network change.","Network Setup",MessageBoxButtons.OK,MessageBoxIcon.Information);
    }
    private void RefreshInvoice()
    {
        var invoice=_viewModel.Invoice;
        if (invoice is null) return;
        _heading.Text=$"Invoice {invoice.FormattedInvoiceNumber} — {invoice.BuyerName} — {invoice.Status}";
        _purchases.DataSource=invoice.Purchases.ToList(); _payments.DataSource=invoice.Payments.ToList(); _pending.DataSource=_viewModel.PendingPayments.ToList();_refunds.DataSource=invoice.Refunds.ToList();
        PrepareSelectedPaymentAmount();RefreshPaymentPreview();
    }
    private void RefreshPaymentPreview(){if(_method.SelectedItem is not PaymentMethod method)return;var fee=_viewModel.CalculateCardFee(method,_amount.Value);_paymentPreview.Text=method==PaymentMethod.Card?$"Invoice {_amount.Value:C2} + card fee ({_viewModel.CardFeeDescription}) {fee:C2} = charge {_amount.Value+fee:C2}":$"{method}: no card fee";_paymentPreview.ForeColor=method==PaymentMethod.Card?Color.DarkRed:Color.DarkGreen;RefreshInvoiceTotals(method,fee);}
    private void RefreshInvoiceTotals(PaymentMethod method,decimal activeFee)
    {
        var invoice=_viewModel.Invoice;if(invoice is null)return;
        var previewFee=_viewModel.PendingCardFee+(method==PaymentMethod.Card?activeFee:0m);
        _totals.Text=$"Hammer {invoice.HammerSubtotal:C2}   Premium {invoice.BuyerPremiumAmount:C2}   Tax {invoice.SalesTaxAmount:C2}   Card fee {invoice.CreditCardFeeAmount+previewFee:C2}{(previewFee>0?$" ({previewFee:C2} pending)":"")}   Invoice total with fee {invoice.TotalAmount+previewFee:C2}   Paid {invoice.AmountPaid:C2}   Invoice balance before fee {_viewModel.RemainingInvoiceAmount:C2}";
    }
    private void PaymentMethodChanged()
    {
        PrepareSelectedPaymentAmount();
        RefreshPaymentPreview();
        RefreshCashChange();
    }
    private void PrepareSelectedPaymentAmount(){if(_method.SelectedItem is not PaymentMethod method)return;var suggested=_viewModel.SuggestedPaymentAmount(method);if(suggested>=0&&suggested<=_amount.Maximum)_amount.Value=suggested;var isCash=method==PaymentMethod.Cash;_cashTendered.Enabled=isCash;_changeDue.Enabled=isCash;if(isCash&&suggested<=_cashTendered.Maximum)_cashTendered.Value=suggested;else if(!isCash)_cashTendered.Value=0;RefreshCashChange();}
    private void RefreshCashChange(){var isCash=_method.SelectedItem is PaymentMethod.Cash;var change=isCash?_viewModel.CalculateCashChange(_amount.Value,_cashTendered.Value):0m;_changeDue.Text=change.ToString("C2");_changeDue.ForeColor=change>0?Color.DarkGreen:Navy;}
    private void RefreshSettlement()
    {
        var settlement=_viewModel.Settlement; if(settlement is null)return;
        _settlementHeading.Text=$"Settlement {settlement.FormattedSettlementNumber} — {settlement.ConsignorName} — {settlement.Status}   Gross {settlement.HammerSubtotal:C2}   Commission/fees {settlement.CommissionAmount:C2}   Buyer purchases deducted {settlement.BuyerPurchaseBalanceDeduction:C2}   Net {settlement.NetAmount:C2}   Paid {settlement.AmountPaid:C2}   Balance {settlement.BalanceDue:C2}{(settlement.HoldCheck?"   HOLD CHECK":"")}";
        _settlementLots.DataSource=settlement.Lots.ToList(); _payouts.DataSource=settlement.Payouts.ToList();
    }
    private static void Execute(Action action) { try { action(); } catch(Exception exception) { MessageBox.Show(exception.Message,"Cashier",MessageBoxButtons.OK,MessageBoxIcon.Error); } }
}
