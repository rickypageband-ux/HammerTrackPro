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

    private static IEnumerable<Control> Descendants(Control control)
    {
        yield return control;
        foreach(Control child in control.Controls)
            foreach(var descendant in Descendants(child))yield return descendant;
    }
}
