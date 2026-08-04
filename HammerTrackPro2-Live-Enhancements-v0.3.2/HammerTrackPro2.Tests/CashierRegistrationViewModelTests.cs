using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Tests;

public sealed class CashierRegistrationViewModelTests
{
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
}
