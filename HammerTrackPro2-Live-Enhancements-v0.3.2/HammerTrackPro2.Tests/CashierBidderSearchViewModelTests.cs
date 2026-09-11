using HammerTrackPro2.Models;
using HammerTrackPro2.ViewModels;
using HammerTrackPro2.Forms;
using System.Windows.Forms;

namespace HammerTrackPro2.Tests;

public sealed class CashierBidderSearchViewModelTests
{
    private static readonly IReadOnlyList<RegisteredBidderSummary> Bidders=[new(){BuyerNumber=14,Name="Alex Morgan",DriversLicense="SC-11114"},new(){BuyerNumber=72,Name="Taylor Reed",DriversLicense="NC-98772"}];
    [Theory]
    [InlineData("72",72)]
    [InlineData("alex",14)]
    [InlineData("98772",72)]
    public void SearchesByBidderNumberNameOrDriversLicense(string query,int expectedNumber){var result=new CashierBidderSearchViewModel(Bidders).Search(query);Assert.Single(result);Assert.Equal(expectedNumber,result[0].BuyerNumber);}
    [Fact]
    public void EmptySearchReturnsAllCurrentAuctionBiddersInNumberOrder(){var result=new CashierBidderSearchViewModel(Bidders.Reverse().ToList()).Search(string.Empty);Assert.Equal([14,72],result.Select(x=>x.BuyerNumber));}
    [Fact]
    public void SearchResultsDoNotDisplayDriversLicenseNumber(){using var form=new CashierBidderSearchForm(new CashierBidderSearchViewModel(Bidders));var grid=form.Controls.Cast<Control>().SelectMany(Descendants).OfType<DataGridView>().Single();Assert.DoesNotContain(grid.Columns.Cast<DataGridViewColumn>(),column=>column.DataPropertyName==nameof(RegisteredBidderSummary.DriversLicense));}

    private static IEnumerable<Control> Descendants(Control control){yield return control;foreach(Control child in control.Controls)foreach(var descendant in Descendants(child))yield return descendant;}
}
