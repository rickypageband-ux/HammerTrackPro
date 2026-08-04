using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Forms;

public sealed class CashierForm : Form
{
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
    private readonly ComboBox _method=new() { DropDownStyle=ComboBoxStyle.DropDownList,DataSource=Enum.GetValues<PaymentMethod>() };
    private readonly NumericUpDown _amount=new() { DecimalPlaces=2,Maximum=100000000 };
    private readonly TextBox _reference=new();
    private readonly NumericUpDown _consignorNumber=new() { Minimum=1,Maximum=999999 };
    private readonly Label _settlementHeading=new() { AutoSize=true };
    private readonly DataGridView _settlementLots=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly DataGridView _payouts=new() { Dock=DockStyle.Fill,ReadOnly=true,AutoGenerateColumns=true,AllowUserToAddRows=false };
    private readonly ComboBox _payoutMethod=new() { DropDownStyle=ComboBoxStyle.DropDownList,DataSource=Enum.GetValues<ConsignorPayoutMethod>() };
    private readonly NumericUpDown _payoutAmount=new() { DecimalPlaces=2,Maximum=100000000 };
    private readonly TextBox _payoutReference=new() { Width=160 };

    public CashierForm(CashierViewModel viewModel,ICashierService cashierService,WorkstationConfiguration? configuration=null)
    {
        _viewModel=viewModel; _cashierService=cashierService; _configuration=configuration; Text="HammerTrack Cashier"; Width=1000; Height=760;
        var lookup=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true };
        lookup.Controls.Add(new Label { Text="Auction Bidder Number",AutoSize=true }); lookup.Controls.Add(_bidderNumber);
        lookup.Controls.Add(Button("Lookup",Lookup)); lookup.Controls.Add(Button("Register Buyer",RegisterBuyer)); lookup.Controls.Add(Button("Preview / Print Invoice",PrintInvoice));
        if(configuration is not null) lookup.Controls.Add(Button("Network Setup",OpenNetworkSetup));
        lookup.Controls.Add(_connection);
        var paymentEntry=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true };
        paymentEntry.Controls.Add(new Label { Text="Method",AutoSize=true }); paymentEntry.Controls.Add(_method);
        paymentEntry.Controls.Add(new Label { Text="Amount",AutoSize=true }); paymentEntry.Controls.Add(_amount);
        paymentEntry.Controls.Add(new Label { Text="Check/Card Reference",AutoSize=true }); paymentEntry.Controls.Add(_reference);
        paymentEntry.Controls.Add(Button("Add Payment",AddPayment)); paymentEntry.Controls.Add(Button("Remove Selected",RemovePayment));
        paymentEntry.Controls.Add(Button("Record Payments",RecordPayments));
        var tabs=new TabControl { Dock=DockStyle.Fill };
        tabs.TabPages.Add(Page("Purchases",_purchases)); tabs.TabPages.Add(Page("Payment History",_payments)); tabs.TabPages.Add(Page("Pending Split Payments",_pending));
        tabs.TabPages.Add(Page("Consignor Lots",_settlementLots)); tabs.TabPages.Add(Page("Consignor Payouts",_payouts));
        var sellerEntry=new FlowLayoutPanel { Dock=DockStyle.Fill,AutoSize=true };
        sellerEntry.Controls.Add(new Label { Text="Consignor Number",AutoSize=true }); sellerEntry.Controls.Add(_consignorNumber); sellerEntry.Controls.Add(Button("Lookup Consignor",LookupConsignor)); sellerEntry.Controls.Add(Button("Register Consignor",RegisterConsignor)); sellerEntry.Controls.Add(Button("Preview / Print Settlement",PrintSettlement));
        sellerEntry.Controls.Add(new Label { Text="Payout Method",AutoSize=true }); sellerEntry.Controls.Add(_payoutMethod); sellerEntry.Controls.Add(new Label { Text="Amount",AutoSize=true }); sellerEntry.Controls.Add(_payoutAmount);
        sellerEntry.Controls.Add(new Label { Text="Check/ACH Reference",AutoSize=true }); sellerEntry.Controls.Add(_payoutReference); sellerEntry.Controls.Add(Button("Record Consignor Payout",RecordConsignorPayout));
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill,RowCount=7,ColumnCount=1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(lookup,0,0); layout.Controls.Add(_heading,0,1); layout.Controls.Add(_totals,0,2); layout.Controls.Add(_settlementHeading,0,3); layout.Controls.Add(tabs,0,4); layout.Controls.Add(paymentEntry,0,5); layout.Controls.Add(sellerEntry,0,6);
        Controls.Add(layout);
        Shown+=async(_,_)=>await CheckConnection(); _connectionTimer.Tick+=async(_,_)=>await CheckConnection(); _connectionTimer.Start();
        FormClosed+=(_,_)=>{_connectionTimer.Stop();_connectionTimer.Dispose();};
    }

    private static TabPage Page(string name,Control content) { var page=new TabPage(name); page.Controls.Add(content); return page; }
    private static Button Button(string text,Action action) { var button=new Button { Text=text,AutoSize=true }; button.Click+=(_,_)=>Execute(action); return button; }
    private void Lookup() { _viewModel.Lookup(Decimal.ToInt32(_bidderNumber.Value)); RefreshInvoice(); }
    private void AddPayment() { _viewModel.AddPayment((PaymentMethod)_method.SelectedItem!,_amount.Value,_reference.Text); _amount.Value=0; _reference.Clear(); RefreshInvoice(); }
    private void RemovePayment() { if (_pending.CurrentRow?.DataBoundItem is PaymentRequest payment) _viewModel.RemovePayment(payment); RefreshInvoice(); }
    private void RecordPayments() { _viewModel.RecordPayments(); RefreshInvoice(); }
    private void LookupConsignor() { _viewModel.LookupConsignor(Decimal.ToInt32(_consignorNumber.Value)); RefreshSettlement(); }
    private void RegisterBuyer(){using var f=new CashierRegistrationForm(false);if(f.ShowDialog(this)!=DialogResult.OK||f.Buyer is null)return;_viewModel.RegisterBuyer(f.Buyer);_bidderNumber.Value=f.Buyer.BidderNumber;RefreshInvoice();}
    private void RegisterConsignor(){using var f=new CashierRegistrationForm(true);if(f.ShowDialog(this)!=DialogResult.OK||f.Consignor is null)return;_viewModel.RegisterConsignor(f.Consignor);_consignorNumber.Value=f.Consignor.ConsignorNumber;RefreshSettlement();}
    private void RecordConsignorPayout()
    {
        var settlement=_viewModel.Settlement??throw new InvalidOperationException("Look up a consignor before recording a payout.");
        if(MessageBox.Show($"Record a {_payoutMethod.SelectedItem} payout of {_payoutAmount.Value:C2} to {settlement.ConsignorName}?","Confirm Consignor Payout",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
        _viewModel.RecordConsignorPayout((ConsignorPayoutMethod)_payoutMethod.SelectedItem!,_payoutAmount.Value,_payoutReference.Text); _payoutAmount.Value=0; _payoutReference.Clear(); RefreshSettlement();
    }
    private void PrintInvoice()
    {
        var invoice=_viewModel.Invoice ?? throw new InvalidOperationException("Look up a bidder before printing.");
        using var document=new InvoicePrinter().CreateDocument(invoice,_cashierService.GetCurrentAuction());
        ShowPrintPreview(document);
    }
    private void PrintSettlement()
    {
        var settlement=_viewModel.Settlement??throw new InvalidOperationException("Look up a consignor before printing a settlement.");
        using var document=new SettlementPrinter().CreateDocument(settlement,_cashierService.GetCurrentAuction());
        ShowPrintPreview(document);
    }
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
        _totals.Text=$"Hammer {invoice.HammerSubtotal:C2}   Premium {invoice.BuyerPremiumAmount:C2}   Tax {invoice.SalesTaxAmount:C2}   Total {invoice.TotalAmount:C2}   Paid {invoice.AmountPaid:C2}   Balance {invoice.BalanceDue:C2}";
        _purchases.DataSource=invoice.Purchases.ToList(); _payments.DataSource=invoice.Payments.ToList(); _pending.DataSource=_viewModel.PendingPayments.ToList();
    }
    private void RefreshSettlement()
    {
        var settlement=_viewModel.Settlement; if(settlement is null)return;
        _settlementHeading.Text=$"Settlement {settlement.FormattedSettlementNumber} — {settlement.ConsignorName} — {settlement.Status}   Hammer {settlement.HammerSubtotal:C2}   Commission {settlement.CommissionAmount:C2} ({settlement.CommissionPercent:0.##}%)   Net {settlement.NetAmount:C2}   Paid {settlement.AmountPaid:C2}   Balance {settlement.BalanceDue:C2}{(settlement.HoldCheck?"   HOLD CHECK":"")}";
        _settlementLots.DataSource=settlement.Lots.ToList(); _payouts.DataSource=settlement.Payouts.ToList();
    }
    private static void Execute(Action action) { try { action(); } catch(Exception exception) { MessageBox.Show(exception.Message,"Cashier",MessageBoxButtons.OK,MessageBoxIcon.Error); } }
}
