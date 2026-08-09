using HammerTrackPro2.Models;
using HammerTrackPro2.Forms;
using HammerTrackPro2.ViewModels;
using HammerTrackPro2.Services;
using System.Windows.Forms;

namespace HammerTrackPro2.Tests;

public sealed class CashierRegistrationViewModelTests
{
    [Fact]
    public void BuyerRegistrationForm_ConstructsWithRegisteredBidderPanel()
    {
        using var form=new CashierRegistrationForm(new CashierRegistrationViewModel(false,[new(){BuyerNumber=7,Name="Alex Buyer"}]));
        Assert.Equal("Register Buyer",form.Text);
        Assert.True(form.Width>=760);
    }

    [Fact]
    public void BuyerRegistration_ShowsUsedNumbersAndRejectsDuplicateSelection()
    {
        var registered=new List<RegisteredBidderSummary>
        {
            new(){BuyerNumber=7,Name="Alex Buyer"},new(){BuyerNumber=25,Name="Pat Bidder"}
        };
        var viewModel=new CashierRegistrationViewModel(false,registered);
        Assert.Equal([7,25],viewModel.RegisteredBidders.Select(x=>x.BuyerNumber));
        var error=Assert.Throws<InvalidOperationException>(()=>viewModel.Create(25,"","New","Buyer","",""));
        Assert.Contains("already registered",error.Message);
        viewModel.Create(12,"","New","Buyer","","");
        Assert.Equal(12,viewModel.Buyer!.BidderNumber);
    }

    [Fact]
    public void SellerRegistrationForm_ShowsExistingBuyerNumbersForLinking()
    {
        using var form=new CashierRegistrationForm(new CashierRegistrationViewModel(true,[new(){BuyerNumber=14,Name="Existing Buyer"}]));
        Assert.Equal("Register Seller",form.Text);
        Assert.True(form.Width>=780);
        Assert.Contains(form.Controls.Cast<Control>().SelectMany(Descendants),x=>x is DataGridView);
    }

    [Fact]
    public void BuyerRegistration_AllowsOptionalDetailsAndCarriesEveryEnteredField()
    {
        var viewModel=new CashierRegistrationViewModel(false);
        viewModel.Create(31,"","","","555-0131","buyer@example.com","31 Main Street","York","PA","17401","PA-123456");
        var buyer=Assert.IsType<AuctionBidder>(viewModel.Buyer);
        Assert.Equal(31,buyer.BidderNumber);Assert.Equal("31 Main Street",buyer.Address);Assert.Equal("York",buyer.City);Assert.Equal("PA",buyer.State);Assert.Equal("17401",buyer.Zip);Assert.Equal("555-0131",buyer.Phone);Assert.Equal("buyer@example.com",buyer.Email);Assert.Equal("PA-123456",buyer.DriversLicense);

        viewModel.Create(32,"","","","","");
        Assert.Equal(32,viewModel.Buyer!.BidderNumber);
        Assert.Equal(string.Empty,viewModel.Buyer.FullName);
    }

    [Fact]
    public void BuyerRegistrationForm_ExposesAllOptionalIdentityAndContactFields()
    {
        using var form=new CashierRegistrationForm(new CashierRegistrationViewModel(false));
        var labels=form.Controls.Cast<Control>().SelectMany(Descendants).OfType<Label>().Select(x=>x.Text).ToList();
        Assert.Contains("Address (optional)",labels);Assert.Contains("Phone Number (optional)",labels);Assert.Contains("Email (optional)",labels);Assert.Contains("Driver's License Number (optional)",labels);
        Assert.Contains("Resale / Tax Exemption Number",labels);Assert.Contains("Exemption State",labels);Assert.Contains("Tax-Exempt Status",labels);
        var buttons=form.Controls.Cast<Control>().SelectMany(Descendants).OfType<Button>().Select(button=>button.Text).ToList();
        Assert.DoesNotContain("Scan with Camera",buttons);Assert.Contains("Scan ID",buttons);Assert.Contains("Scan New ID",buttons);
    }

    [Fact]
    public void BuyerRegistration_CarriesTaxExemptionInformation()
    {
        var viewModel=new CashierRegistrationViewModel(false);
        viewModel.Create(88,"","Tax","Buyer","","",resaleLicenseNumber:"SC-88",resaleLicenseState:"SC",taxExempt:true);
        Assert.True(viewModel.Buyer!.TaxExempt);Assert.Equal("SC-88",viewModel.Buyer.ResaleLicenseNumber);Assert.Equal("SC",viewModel.Buyer.ResaleLicenseState);
    }

    [Fact]
    public void CashierPaymentActions_StayTogetherInAReservedNonWrappingRow()
    {
        var service=new CashierFormService();
        using var form=new CashierForm(new CashierViewModel(service),service);
        var record=form.Controls.Cast<Control>().SelectMany(Descendants).OfType<Button>().Single(x=>x.Text=="Record Payments");
        var actions=Assert.IsType<FlowLayoutPanel>(record.Parent);
        Assert.False(actions.WrapContents);
        Assert.Equal(3,actions.Controls.OfType<Button>().Count());
        Assert.True(actions.Width>=450);
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        yield return control;
        foreach(Control child in control.Controls)
            foreach(var descendant in Descendants(child))yield return descendant;
    }

    private sealed class CashierFormService : ICashierService
    {
        public Auction GetCurrentAuction()=>new(){AuctionNumber="1",AuctionName="Test"};
        public Invoice LoadInvoice(int bidderNumber)=>throw new NotSupportedException();
        public Invoice RecordPayments(int bidderNumber,IReadOnlyList<PaymentRequest> payments)=>throw new NotSupportedException();
        public Invoice RegisterBuyer(AuctionBidder bidder)=>throw new NotSupportedException();
        public IReadOnlyList<RegisteredBidderSummary> GetRegisteredBidders()=>[];
        public ConsignorSettlement LoadConsignorSettlement(int consignorNumber)=>throw new NotSupportedException();
        public ConsignorSettlement RecordConsignorPayout(int consignorNumber,ConsignorPayoutRequest payout)=>throw new NotSupportedException();
        public ConsignorSettlement RegisterConsignor(Consignor consignor)=>throw new NotSupportedException();
        public IReadOnlyList<UnpaidInvoiceSummary> GetUnpaidInvoices()=>[];
        public AuctionSettlementReport GetAuctionSettlementReport()=>throw new NotSupportedException();
        public StaffPayment RecordStaffPayment(StaffPayment payment)=>throw new NotSupportedException();
        public StaffPayment VoidStaffPayment(long staffPaymentId,string reason,StaffPayment? replacement)=>throw new NotSupportedException();
        public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken=default)=>Task.FromResult(true);
    }
}
