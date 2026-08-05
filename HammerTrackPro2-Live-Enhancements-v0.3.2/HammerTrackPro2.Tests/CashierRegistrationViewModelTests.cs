using HammerTrackPro2.Models;
using HammerTrackPro2.Forms;
using HammerTrackPro2.ViewModels;
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
        var buttons=form.Controls.Cast<Control>().SelectMany(Descendants).OfType<Button>().Select(button=>button.Text).ToList();
        Assert.Contains("Scan with Camera",buttons);Assert.Contains("Use Handheld Scanner",buttons);
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        yield return control;
        foreach(Control child in control.Controls)
            foreach(var descendant in Descendants(child))yield return descendant;
    }
}
