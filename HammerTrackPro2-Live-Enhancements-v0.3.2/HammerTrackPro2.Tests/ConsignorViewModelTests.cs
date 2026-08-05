using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using HammerTrackPro2.Forms;
using System.Windows.Forms;

namespace HammerTrackPro2.Tests;

public sealed class ConsignorViewModelTests
{
    [Fact]
    public void SellerManagement_UsesBrandedUniformActionsAndFocusedDirectory()
    {
        using var form=new ConsignorForm(new ConsignorService(new InMemoryConsignorRepository()));
        var controls=form.Controls.Cast<Control>().SelectMany(Descendants).ToList();
        var buttons=controls.OfType<Button>().Where(x=>x.Text is "Add Seller" or "Save Seller" or "Delete Seller").ToList();
        Assert.Equal("HammerTrack Pro - Seller Management",form.Text);
        Assert.Equal(3,buttons.Count);
        Assert.All(buttons,x=>Assert.Equal(42,x.Height));
        Assert.Contains(controls.OfType<Button>(),x=>x.Text=="Full Screen");
        var grid=Assert.Single(controls.OfType<DataGridView>());
        Assert.DoesNotContain(grid.Columns.Cast<DataGridViewColumn>(),x=>x.DataPropertyName==nameof(Consignor.TaxId));
    }

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

    private static IEnumerable<Control> Descendants(Control control)
    {
        yield return control;
        foreach(Control child in control.Controls)
            foreach(var descendant in Descendants(child))yield return descendant;
    }
}
