using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using HammerTrackPro2.Forms;
using Microsoft.Data.Sqlite;
using System.Windows.Forms;

namespace HammerTrackPro2.Tests;

public sealed class SoldItemSearchTests
{
    [Fact]
    public void Search_MatchesDescriptionOrLotAndKeepsDifferentPricesDistinct()
    {
        using var f = new Fixture();
        f.Service.RecordSale(1, 101, 2.35m, true, 2);
        f.Service.RecordSale(1, 102, 3.15m, true, 2);
        f.Service.RecordSale(1, 102, 3.15m, true, 2);
        f.Service.CloseLot(1);
        var results = f.Service.SearchSoldItems(" TOWELS ", AuctionSaleMode.Retail);
        Assert.Equal(2, results.Count);
        Assert.Equal(new[] { 3.15m, 2.35m }, results.Select(x => x.PriceEach));
        Assert.Equal(2, f.Service.SearchSoldItems("1", AuctionSaleMode.Retail).Count);
        Assert.Empty(f.Service.SearchSoldItems("%", AuctionSaleMode.Retail));
        Assert.Empty(f.Service.SearchSoldItems("towels", AuctionSaleMode.Wholesale));
        Assert.Empty(f.Service.SearchSoldItems("unsold", AuctionSaleMode.Retail));
    }

    [Theory]
    [InlineData(AuctionSaleMode.Retail, 3, 3, 1)]
    [InlineData(AuctionSaleMode.Wholesale, 3, 12, 3)]
    public void AdditionalBuyer_PreservesSourceAndClosedLotAndUpdatesInvoice(AuctionSaleMode mode, int count, int total, int deals)
    {
        using var f = new Fixture(mode);
        var original = f.Service.RecordSale(1, 101, 2.35m, false, 4, 1);
        var vm = new SoldItemSearchViewModel(f.Service, mode);
        vm.Search("towels"); vm.Select(Assert.Single(vm.Results)); vm.BidderNumber = 102; vm.QuantityOrDeals = count;
        Assert.Equal(total * 2.35m, vm.Total);
        var added = vm.Record();
        Assert.Equal(total, added.Quantity); Assert.Equal(deals, added.Deals); Assert.Equal(2.35m, added.HammerPrice);
        Assert.Equal(mode == AuctionSaleMode.Wholesale ? 4 : 3, added.QuantityPerDeal);
        Assert.Equal("Sold", f.Service.Load().Lots.Single(x => x.LotNumber == 1).Status);
        var reopened = new SqliteDataStore(f.Path);
        Assert.Equal(2, reopened.GetSalesForLot(1).Count);
        Assert.Equal(original.TotalPrice, reopened.GetSalesForLot(1).Single(x => x.Id == original.Id).TotalPrice);
        Assert.Equal(101, reopened.GetSalesForLot(1).Single(x => x.Id == original.Id).BidderNumber);
        Assert.Equal(total * 2.35m, reopened.LoadCashierInvoice(102).HammerSubtotal);
        Assert.Equal("1", f.Scalar("SELECT COUNT(*) FROM AuditLog WHERE Action='AdditionalBuyerRecorded'"));
        Assert.Null(f.Scalar("PRAGMA foreign_key_check"));
        Assert.Throws<InvalidOperationException>(() => vm.Record());
        Assert.True(File.Exists(f.Service.Backup()));
    }

    [Fact]
    public void AdditionalBuyer_RejectsInvalidBidderAndQuantityWithoutWriting()
    {
        using var f = new Fixture(); var sale = f.Service.RecordSale(1, 101, 5m);
        Assert.Throws<InvalidOperationException>(() => f.Service.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 999, 1));
        Assert.Throws<InvalidOperationException>(() => f.Service.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, 0));
        Assert.Throws<InvalidOperationException>(() => f.Store.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, -1));
        Assert.Single(f.Service.GetSalesForLot(1));
        Assert.Equal("0", f.Scalar("SELECT COUNT(*) FROM AuditLog WHERE Action='AdditionalBuyerRecorded'"));
    }

    [Fact]
    public void AdditionalBuyer_RejectsClosedAuctionAtBothLayers()
    {
        using var f = new Fixture(); var sale = f.Service.RecordSale(1, 101, 5m);
        f.Service.CloseAuction(sale.AuctionId);
        Assert.Throws<InvalidOperationException>(() => f.Service.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, 1));
        Assert.Throws<InvalidOperationException>(() => f.Store.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, 1));
        Assert.Single(f.Service.GetSalesForLot(1));
    }

    [Fact]
    public void AdditionalBuyer_RejectsStaleSelectionAfterAuctionChangeOrModeMismatch()
    {
        using var f = new Fixture(); var sale = f.Service.RecordSale(1, 101, 5m);
        Assert.Throws<InvalidOperationException>(() => f.Service.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Wholesale, 102, 1));
        var next = f.Service.SaveAuction(new Auction { AuctionNumber = "NEXT", AuctionName = "Next" });
        f.Service.SelectAuction(next.AuctionId);
        Assert.Empty(f.Service.SearchSoldItems("towels", AuctionSaleMode.Retail));
        Assert.Throws<InvalidOperationException>(() => f.Service.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, 1));
        Assert.Throws<InvalidOperationException>(() => f.Store.RecordAdditionalBuyer(sale.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, 1));
        Assert.Throws<InvalidOperationException>(() => f.Store.RecordAdditionalBuyer(next.AuctionId, sale.Id, AuctionSaleMode.Retail, 102, 1));
    }

    [Fact]
    public void SelectionResetAndNoSelectionPreventAccidentalSale()
    {
        using var f = new Fixture(); f.Service.RecordSale(1, 101, 5m);
        var vm = new SoldItemSearchViewModel(f.Service, AuctionSaleMode.Retail);
        vm.Search("towels"); vm.Select(Assert.Single(vm.Results));
        vm.Search("missing"); Assert.Null(vm.SelectedItem);
        Assert.Throws<InvalidOperationException>(() => vm.Record());
        Assert.Single(f.Service.GetSalesForLot(1));
    }

    [Fact]
    public void DialogAndClerkExposeSearchAndLockedPriceWithoutEditablePriceField()
    {
        using var f = new Fixture();
        using var form = new SoldItemSearchForm(new SoldItemSearchViewModel(f.Service, AuctionSaleMode.Retail));
        Assert.Equal(2, Descendants(form).OfType<NumericUpDown>().Count());
        using var clerk = new LiveAuctionForm(f.Service, new LiveAuctionViewModel(f.Service, new ConsignorService(f.Store)));
        Assert.Single(Descendants(clerk).OfType<Button>(), x => x.Text == "SEARCH SOLD ITEMS");
    }

    [Fact]
    public void PaidSourceAndBuyer_KeepPaymentsAndGainOnlyNewBalance()
    {
        using var f = new Fixture(); var source = f.Service.RecordSale(1, 101, 5m);
        var cashier = new CashierService(f.Store);
        var before = cashier.LoadInvoice(101);
        var paid = cashier.RecordPayments(101, [new PaymentRequest { Method = PaymentMethod.Cash, Amount = before.BalanceDue }]);
        var payment = Assert.Single(paid.Payments);
        f.Service.RecordAdditionalBuyer(source.AuctionId, source.Id, AuctionSaleMode.Retail, 102, 1);
        Assert.True(f.Service.GetSalesForLot(1).Single(x => x.Id == source.Id).Paid);
        f.Service.RecordAdditionalBuyer(source.AuctionId, source.Id, AuctionSaleMode.Retail, 101, 2);
        var after = cashier.LoadInvoice(101);
        Assert.Equal(15m, after.HammerSubtotal);
        Assert.Equal(paid.AmountPaid, after.AmountPaid);
        Assert.Equal(payment.AppliedAmount, Assert.Single(after.Payments).AppliedAmount);
        Assert.True(after.BalanceDue > 0);
        // Paid is an invoice-wide derived flag; the original financial amounts remain intact.
        var retained = f.Service.GetSalesForLot(1).Single(x => x.Id == source.Id);
        Assert.False(retained.Paid);
        Assert.Equal(source.HammerPrice, retained.HammerPrice);
        Assert.Equal(source.Quantity, retained.Quantity);
    }

    [Fact]
    public void VoidedOrCorrectedSource_IsNotReusableFromStaleSearch()
    {
        using var f = new Fixture(); var source = f.Service.RecordSale(1, 101, 5m);
        var vm = new SoldItemSearchViewModel(f.Service, AuctionSaleMode.Retail);
        vm.Search("towels"); vm.Select(Assert.Single(vm.Results)); vm.BidderNumber = 102;
        f.Service.CorrectSale(101, new SaleCorrectionRequest { SaleId = source.Id, BidderNumber = 101, Quantity = 1, HammerPrice = 7m, Reason = "Price correction" });
        Assert.Throws<InvalidOperationException>(() => vm.Record());
        Assert.Equal(7m, Assert.Single(f.Service.SearchSoldItems("towels", AuctionSaleMode.Retail)).PriceEach);
    }

    [Fact]
    public void AdditionalBuyer_CanBeUndoneWithoutRemovingOriginalPurchase()
    {
        using var f = new Fixture(); var source = f.Service.RecordSale(1, 101, 5m);
        var added = f.Service.RecordAdditionalBuyer(source.AuctionId, source.Id, AuctionSaleMode.Retail, 102, 2);
        f.Service.UndoLastSale(added.Id);
        Assert.Equal(source.Id, Assert.Single(f.Service.GetSalesForLot(1)).Id);
        Assert.Equal(0m, f.Store.LoadCashierInvoice(102).HammerSubtotal);
        Assert.Equal("1", f.Scalar("SELECT COUNT(*) FROM AuditLog WHERE Action='SaleUndone'"));
    }

    [Fact]
    public void WholesaleOverflow_IsRejectedBeforeInsert()
    {
        using var f = new Fixture(AuctionSaleMode.Wholesale);
        var source = f.Service.RecordSale(1, 101, 1m, false, 2);
        Assert.Throws<OverflowException>(() => f.Service.RecordAdditionalBuyer(source.AuctionId, source.Id, AuctionSaleMode.Wholesale, 102, int.MaxValue));
        Assert.Single(f.Service.GetSalesForLot(1));
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HammerTrackSoldItems", Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(_folder, "test.db");
        public SqliteDataStore Store { get; }
        public AuctionService Service { get; }
        public Fixture(AuctionSaleMode mode = AuctionSaleMode.Retail)
        {
            Directory.CreateDirectory(_folder); Store = new SqliteDataStore(Path); Service = new AuctionService(Store);
            var auction = Service.GetCurrentAuction();
            foreach (var number in new[] { 101, 102 }) Service.SaveBidder(new AuctionBidder { AuctionId = auction.AuctionId, BidderNumber = number, FirstName = "Test" }, null);
            var seller = new ConsignorService(Store).GetByNumber(1);
            Service.SaveLot(new AuctionLot { AuctionId = auction.AuctionId, ConsignorId = seller.ConsignorId, LotNumber = 1, Description = "Blue towels", SaleMode = mode }, null);
            Service.SaveLot(new AuctionLot { AuctionId = auction.AuctionId, ConsignorId = seller.ConsignorId, LotNumber = 2, Description = "Unsold", SaleMode = mode }, null);
        }
        public string? Scalar(string sql) { using var c = new SqliteConnection($"Data Source={Path}"); c.Open(); using var q = c.CreateCommand(); q.CommandText = sql; return q.ExecuteScalar()?.ToString(); }
        public void Dispose() { SqliteConnection.ClearAllPools(); Directory.Delete(_folder, true); }
    }
}
