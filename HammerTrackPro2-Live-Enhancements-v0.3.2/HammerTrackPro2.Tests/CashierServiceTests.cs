using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Tests;

public sealed class CashierServiceTests
{
    [Fact] public void AuctionSettlement_IncludesTaxAndAppendOnlyStaffCorrections(){using var db=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(db.Path,SalesTaxBasis.HammerOnly);var p=cashier.RecordStaffPayment(new StaffPayment{Category="Clerk",Payee="Pat",Amount=50,Method="Cash"});cashier.VoidStaffPayment(p.StaffPaymentId,"Wrong amount",new StaffPayment{Category="Clerk",Payee="Pat",Amount=40,Method="Cash"});var r=cashier.GetAuctionSettlementReport();Assert.Equal(5,r.SalesTaxCollected);Assert.Equal(40,r.StaffPayments);Assert.Equal(3,r.StaffLedger.Count);Assert.Contains(r.StaffLedger,x=>x.VoidsStaffPaymentId==p.StaffPaymentId&&x.Amount==-50);}
    [Fact] public void CardFee_AppliesOnlyToCardPortion(){using var db=TemporaryDatabase.Create();var (_,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);var invoice=cashier.RecordPayments(25,[new PaymentRequest{Method=PaymentMethod.Card,Amount=50},new PaymentRequest{Method=PaymentMethod.Cash,Amount=20}]);Assert.Equal(1.5m,invoice.CreditCardFeeAmount);Assert.Equal(111.5m,invoice.TotalAmount);Assert.Equal(40m,invoice.BalanceDue);Assert.Equal(0,invoice.Payments.Single(x=>x.Method==PaymentMethod.Cash).ProcessingFee);}
    [Fact] public void LinkedConsignorBuyer_UnpaidPurchasesReducePayout(){using var db=TemporaryDatabase.Create();var store=new SqliteDataStore(db.Path);var auction=store.GetCurrentAuction();var cs=new ConsignorService(store);cs.SaveConsignor(new Consignor{ConsignorNumber=70,BusinessName="Buyer Seller",Active=true},null);var linked=store.GetConsignors().Single(x=>x.ConsignorNumber==70);Assert.NotNull(linked.LinkedBuyerId);var owner=store.GetConsignors().First();store.SaveLot(new AuctionLot{AuctionId=auction.AuctionId,ConsignorId=owner.ConsignorId,LotNumber=1,Description="Purchase"},null);store.RecordSale(1,70,100);store.SaveLot(new AuctionLot{AuctionId=auction.AuctionId,ConsignorId=linked.ConsignorId,LotNumber=2,Description="Consigned"},null);store.SaveBidder(new AuctionBidder{AuctionId=auction.AuctionId,BidderNumber=71,FirstName="Other"},null);store.RecordSale(2,71,200);var settlement=new CashierService(store).LoadConsignorSettlement(70);Assert.Equal(100,settlement.BuyerPurchaseBalanceDeduction);Assert.Equal(100,settlement.NetAmount);}
    [Fact] public void UnpaidReview_UsesFreshCurrentAuctionBalances(){using var db=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);Assert.Single(cashier.GetUnpaidInvoices());cashier.RecordPayments(25,[new PaymentRequest{Method=PaymentMethod.Cash,Amount=110}]);Assert.Empty(cashier.GetUnpaidInvoices());}
    [Fact]
    public void CashierRegistration_CreatesAndSelectsCurrentAuctionRecords()
    {using var db=TemporaryDatabase.Create();var store=new SqliteDataStore(db.Path);var cashier=new CashierService(store);Assert.Equal(41,cashier.RegisterBuyer(new AuctionBidder{BidderNumber=41,FirstName="New",LastName="Buyer"}).BidderNumber);Assert.Equal(42,cashier.RegisterConsignor(new Consignor{ConsignorNumber=42,BusinessName="Farm"}).ConsignorNumber);Assert.Throws<InvalidOperationException>(()=>cashier.RegisterBuyer(new AuctionBidder{BidderNumber=41,FirstName="Dup"}));Assert.Throws<InvalidOperationException>(()=>cashier.RegisterConsignor(new Consignor{ConsignorNumber=42,BusinessName="Dup"}));}

    [Theory]
    [InlineData(SalesTaxBasis.None, 110)]
    [InlineData(SalesTaxBasis.HammerOnly, 115)]
    [InlineData(SalesTaxBasis.HammerPlusBuyerPremium, 115.5)]
    public void Invoice_UsesCurrentAuctionCommercialTerms(SalesTaxBasis basis, decimal expectedTotal)
    {
        using var database=TemporaryDatabase.Create();
        var (store,cashier)=CreateAccount(database.Path,basis);
        var invoice=cashier.LoadInvoice(25);
        Assert.Equal(100m,invoice.HammerSubtotal); Assert.Equal(10m,invoice.BuyerPremiumAmount);
        Assert.Equal(expectedTotal,invoice.TotalAmount); Assert.Equal(expectedTotal,invoice.BalanceDue);
        Assert.Equal("A-001-0025",invoice.FormattedInvoiceNumber);
        Assert.Equal("Unpaid",invoice.Status);
        Assert.Single(invoice.Purchases);
    }

    [Fact]
    public void Payments_SupportPartialSplitAndCashChange_AndRejectNonCashOverpayment()
    {
        using var database=TemporaryDatabase.Create();
        var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.HammerPlusBuyerPremium);
        var partial=cashier.RecordPayments(25,[new PaymentRequest { Method=PaymentMethod.Check,Amount=50,Reference="123" }]);
        Assert.Equal("Partially Paid",partial.Status); Assert.Equal(65.5m,partial.BalanceDue);
        Assert.Throws<InvalidOperationException>(()=>cashier.RecordPayments(25,[new PaymentRequest { Method=PaymentMethod.Card,Amount=66 }]));

        var paid=cashier.RecordPayments(25,[new PaymentRequest { Method=PaymentMethod.Card,Amount=20 },new PaymentRequest { Method=PaymentMethod.Cash,Amount=50 }]);
        Assert.Equal("Paid",paid.Status); Assert.Equal(0,paid.BalanceDue); Assert.Equal(4.5m,paid.Payments.Last().ChangeDue);
        Assert.Equal("3",Scalar(database.Path,"SELECT COUNT(*) FROM Payments;"));
        Assert.Equal("1",Scalar(database.Path,"SELECT Paid FROM Sales LIMIT 1;"));
        var saleId=long.Parse(Scalar(database.Path,"SELECT Id FROM Sales LIMIT 1;")!);
        Assert.Throws<InvalidOperationException>(()=>store.UndoLastSale(saleId));
    }

    [Fact]
    public void UnpaidInvoice_RefreshesTermsFromCurrentAuction()
    {
        using var database=TemporaryDatabase.Create(); var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.HammerOnly);
        var auction=store.GetCurrentAuction(); auction.BuyerPremiumPercent=20; auction.SalesTaxPercent=0; store.SaveAuction(auction);
        var invoice=cashier.LoadInvoice(25);
        Assert.Equal(20m,invoice.BuyerPremiumAmount); Assert.Equal(120m,invoice.TotalAmount);
    }

    [Fact]
    public void EmptyInvoice_DoesNotPreventRemovingAuctionRegistration()
    {
        using var database=TemporaryDatabase.Create(); var store=new SqliteDataStore(database.Path); var auction=store.GetCurrentAuction();
        store.SaveBidder(new AuctionBidder { AuctionId=auction.AuctionId,BidderNumber=9,FirstName="No",LastName="Purchases" },null);
        store.DeleteBidder(9);
        Assert.Empty(store.Load().Bidders); Assert.Single(store.GetBuyers());
    }

    [Fact]
    public void ConsignorSettlement_UsesCommissionPrecedence_AndRequiresClosedAuction()
    {
        using var database=TemporaryDatabase.Create(); var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.None);
        var consignor=store.GetConsignors().First(); consignor.DefaultCommissionPercent=12; consignor.CommissionOverridePercent=8; store.SaveConsignor(consignor,consignor.ConsignorNumber);
        var preview=cashier.LoadConsignorSettlement(consignor.ConsignorNumber);
        Assert.Equal(100m,preview.HammerSubtotal); Assert.Equal(8m,preview.CommissionAmount); Assert.Equal(92m,preview.NetAmount);
        Assert.Throws<InvalidOperationException>(()=>cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Cash,Amount=20 }));
        store.CloseAuction(store.GetCurrentAuction().AuctionId);
        var paid=cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Cash,Amount=20 });
        Assert.Equal("Partially Paid",paid.Status); Assert.Equal(72m,paid.BalanceDue);
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM ConsignorPayouts;"));
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM AuditLog WHERE Action='ConsignorPayoutRecorded';"));
    }

    [Fact]
    public void ConsignorSettlement_ValidatesHoldCheckReferencesAndOverpayment()
    {
        using var database=TemporaryDatabase.Create(); var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.None);
        var consignor=store.GetConsignors().First(); consignor.HoldCheck=true; store.SaveConsignor(consignor,consignor.ConsignorNumber); store.CloseAuction(store.GetCurrentAuction().AuctionId);
        Assert.Throws<InvalidOperationException>(()=>cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Check,Amount=10,Reference="100" }));
        Assert.Throws<InvalidOperationException>(()=>cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Ach,Amount=10 }));
        Assert.Throws<InvalidOperationException>(()=>cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Cash,Amount=101 }));
        var paid=cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Ach,Amount=100,Reference="ACH-42" });
        Assert.Equal("Paid",paid.Status); Assert.Equal(0,paid.BalanceDue);
    }

    private static (SqliteDataStore Store,CashierService Cashier) CreateAccount(string path,SalesTaxBasis basis)
    {
        var store=new SqliteDataStore(path); var auction=store.GetCurrentAuction();
        auction.AuctionNumber="A-001"; auction.AuctionName="Test Auction"; auction.BuyerPremiumPercent=10; auction.SalesTaxPercent=5; auction.SalesTaxBasis=basis;
        store.SaveAuction(auction);
        store.SaveBidder(new AuctionBidder { AuctionId=auction.AuctionId,BidderNumber=25,FirstName="Alex",LastName="Buyer" },null);
        var consignor=store.GetConsignors().First();
        store.SaveLot(new AuctionLot { AuctionId=auction.AuctionId,ConsignorId=consignor.ConsignorId,LotNumber=1,Description="Tractor" },null);
        store.RecordSale(1,25,100);
        return (store,new CashierService(store));
    }

    private static string? Scalar(string path,string sql)
    {
        using var connection=new SqliteConnection($"Data Source={path}"); connection.Open(); using var command=connection.CreateCommand(); command.CommandText=sql; return command.ExecuteScalar()?.ToString();
    }
    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _folder; private TemporaryDatabase(string folder) { _folder=folder; Path=System.IO.Path.Combine(folder,"test.db"); }
        public string Path { get; }
        public static TemporaryDatabase Create() { var folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"HammerTrackPro2.Tests",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); return new(folder); }
        public void Dispose() { SqliteConnection.ClearAllPools(); Directory.Delete(_folder,true); }
    }
}
