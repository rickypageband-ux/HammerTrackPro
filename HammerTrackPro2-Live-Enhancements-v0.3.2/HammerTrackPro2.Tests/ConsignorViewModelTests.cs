using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Tests;

public sealed class ConsignorViewModelTests
{
    [Theory]
    [InlineData("44")]
    [InlineData("Oak Hill")]
    [InlineData("Morgan Reed")]
    [InlineData("555-0100")]
    [InlineData("morgan@example.com")]
    [InlineData("TAX-44")]
    public void Search_FindsSupportedConsignorFields(string query)
    {
        var repository = new InMemoryConsignorRepository();
        repository.SaveConsignor(new Consignor
        {
            ConsignorNumber = 44,
            BusinessName = "Oak Hill",
            FirstName = "Morgan",
            LastName = "Reed",
            Phone = "555-0100",
            Email = "morgan@example.com",
            TaxId = "TAX-44",
            Active = true
        }, null);
        var viewModel = new ConsignorViewModel(new ConsignorService(repository));
        viewModel.Load();

        viewModel.SearchText = query;
        viewModel.Search();

        Assert.Single(viewModel.Consignors);
    }
}
