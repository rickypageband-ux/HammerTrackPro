using System.Drawing;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Tests;

public sealed class CashierServiceTests
{
    [Fact] public void EndOfNightLayout_UsesReadableScaledHeadersMetricsAndScrollableContent(){Assert.Equal(6,HammerTrackPro2.Forms.AuctionSettlementForm.MetricColumnCount);Assert.Equal(2,HammerTrackPro2.Forms.AuctionSettlementForm.MetricRowCount);Assert.True(HammerTrackPro2.Forms.AuctionSettlementForm.SectionHeaderHeight>=74);Assert.True(HammerTrackPro2.Forms.AuctionSettlementForm.ScrollableContentHeight>=920);}
    [Fact] public void SellerSettlementPrint_UsesCompactBuyerDeductionLabel()=>Assert.Equal("Buyer balance deducted",SettlementPrinter.BuyerDeductionLabel);
    [Fact] public void SellerItemSummary_GroupsMatchingLotBidderPriceRowsAndTotalsActualQuantity(){var settlement=new ConsignorSettlement{HammerSubtotal=52.50m,Lots=[new(){LotNumber=1,Description="Car",BidderNumber=501,Quantity=6,HammerPrice=5},new(){LotNumber=1,Description="Car",BidderNumber=501,Quantity=3,HammerPrice=5},new(){LotNumber=1,Description="Car",BidderNumber=502,Quantity=1,HammerPrice=7.50m},new(){LotNumber=2,Description="Chair",BidderNumber=503,Quantity=2,HammerPrice=7.50m}]};var rows=SellerItemSummaryPrinter.BuildRows(settlement);Assert.Equal(3,rows.Count);Assert.Equal(501,rows[0].BidderNumber);Assert.Equal(9,rows[0].Quantity);Assert.Equal(45m,rows[0].Total);Assert.Equal(12,SellerItemSummaryPrinter.TotalItemsSold(settlement));using var bitmap=new Bitmap(850,1100);using var graphics=Graphics.FromImage(bitmap);Assert.Equal(3,SellerItemSummaryPrinter.Draw(graphics,new Rectangle(40,40,770,1020),settlement,new Auction{AuctionName="Auction"},rows));}
    [Fact] public void TaxExemptBuyer_PersistsLicenseAndReceivesNoSalesTax(){using var db=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(db.Path,SalesTaxBasis.HammerOnly);var bidder=store.Load().Bidders.Single(x=>x.BidderNumber==25);bidder.TaxExempt=true;bidder.ResaleLicenseNumber="SC-RESALE-25";bidder.ResaleLicenseState="SC";store.SaveBidder(bidder,25);var invoice=cashier.LoadInvoice(25);Assert.True(invoice.BuyerTaxExempt);Assert.Equal("SC-RESALE-25",invoice.BuyerResaleLicenseNumber);Assert.Equal("SC",invoice.BuyerResaleLicenseState);Assert.Equal(0,invoice.SalesTaxAmount);Assert.Equal(110,invoice.TotalAmount);var stored=Assert.Single(store.GetBuyers());Assert.True(stored.TaxExempt);Assert.Equal("SC-RESALE-25",stored.ResaleLicenseNumber);}
    [Fact] public void InvoiceWarrantyDisclaimer_PreservesApprovedText(){Assert.Contains("Condition of Sale – Disclaimer of Warranties",InvoicePrinter.WarrantyDisclaimer);Assert.Contains("\"AS IS\" and \"WHERE IS,\"",InvoicePrinter.WarrantyDisclaimer);Assert.Contains("hold the auction company harmless",InvoicePrinter.WarrantyDisclaimer);Assert.Equal(3,InvoicePrinter.WarrantyDisclaimer.Split("\n\n").Length);}
    [Fact] public void ProfessionalPrintLayouts_EmbedBrandingAndReserveReadableColumns(){using var logo=PrintBranding.LoadLogo();Assert.True(logo.Width>0&&logo.Height>0);Assert.True(PrintBranding.DocumentTitleWidth>=340);Assert.True(InvoicePrinter.QuantityColumnWidth>=90);var auction=new Auction{AuctionNumber="A-1",AuctionName="Estate Auction",AuctionDate=new DateTime(2026,8,5),CompanyName="RPDAVID Auctions",CompanyAddress="123 Main St",CompanyCity="Town",CompanyState="PA",CompanyZip="19000",CompanyPhone="555-0100",CompanyEmail="office@example.com"};var invoice=new Invoice{AuctionNumber="A-1",BidderNumber=25,BuyerName="Taylor Buyer",BuyerAddress="9 Buyer Lane",BuyerPhone="555-0125",BuyerEmail="buyer@example.com",Status="Paid",HammerSubtotal=100,TotalAmount=110,AmountPaid=110,Purchases=[new InvoiceLine{LotNumber=1,Description="Hammer",Quantity=120,HammerPrice=100}]};var settlement=new ConsignorSettlement{AuctionNumber="A-1",ConsignorNumber=12,ConsignorName="Sample Seller",ConsignorAddress="15 Seller Road",Status="Unpaid",HammerSubtotal=12000,CommissionPercent=20,CommissionAmount=2400,NetAmount=9600,BalanceDue=9600,Lots=[new(){LotNumber=1,Description="Hammer",BidderNumber=25,Quantity=120,HammerPrice=100}]};using var bitmap=new Bitmap(850,1100);using var graphics=Graphics.FromImage(bitmap);var bounds=new Rectangle(40,40,770,1020);Assert.Equal(1,InvoicePrinter.DrawInvoice(graphics,bounds,invoice,auction));Assert.Equal(1,SettlementPrinter.Draw(graphics,bounds,settlement,auction));var rows=SellerItemSummaryPrinter.BuildRows(settlement);Assert.Equal(1,SellerItemSummaryPrinter.Draw(graphics,bounds,settlement,auction,rows));AuctionSettlementPrinter.Draw(graphics,bounds,new AuctionSettlementReport{AuctionNumber="A-1",AuctionName="Estate Auction",HammerSales=100,InvoiceTotal=110,InvoiceAmountPaid=110},auction);Assert.Equal("INVOICE / RECEIPT",InvoicePrinter.DocumentHeading);Assert.Equal("SELLER SETTLEMENT",SettlementPrinter.DocumentHeading);Assert.Equal("SELLER ITEM SUMMARY",SellerItemSummaryPrinter.DocumentHeading);Assert.Equal("END-OF-NIGHT REPORT",AuctionSettlementPrinter.DocumentHeading);}
    [Fact] public void AuctionSettlement_IncludesTaxAndAppendOnlyStaffCorrections(){using var db=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(db.Path,SalesTaxBasis.HammerOnly);var p=cashier.RecordStaffPayment(new StaffPayment{Category="Clerk",Payee="Pat",Amount=50,Method="Cash"});cashier.VoidStaffPayment(p.StaffPaymentId,"Wrong amount",new StaffPayment{Category="Clerk",Payee="Pat",Amount=40,Method="Cash"});var r=cashier.GetAuctionSettlementReport();Assert.Equal(5,r.SalesTaxCollected);Assert.Equal(40,r.StaffPayments);Assert.Equal(3,r.StaffLedger.Count);Assert.Contains(r.StaffLedger,x=>x.VoidsStaffPaymentId==p.StaffPaymentId&&x.Amount==-50);}
    [Fact] public void AuctionSettlementViewModel_RefreshesDashboardAfterRecordAndVoid(){using var db=TemporaryDatabase.Create();var (_,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);var vm=new HammerTrackPro2.ViewModels.AuctionSettlementViewModel(cashier);vm.Refresh();Assert.Equal(0,vm.Report.StaffPayments);vm.RecordStaffPayment("Clerk"," Pat ",50,"Cash"," Night clerk ");var payment=Assert.Single(vm.Report.StaffLedger);Assert.Equal("Pat",payment.Payee);Assert.Equal(50,vm.Report.StaffPayments);vm.VoidStaffPayment(payment,"Entered twice");Assert.Equal(0,vm.Report.StaffPayments);Assert.Equal(2,vm.Report.StaffLedger.Count);}
    [Fact] public void CardFee_AppliesTwoPointSixFivePercentPlusFifteenCentsOnlyToCardPortion(){using var db=TemporaryDatabase.Create();var (_,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);var invoice=cashier.RecordPayments(25,[new PaymentRequest{Method=PaymentMethod.Card,Amount=50},new PaymentRequest{Method=PaymentMethod.Cash,Amount=20}]);Assert.Equal(1.48m,invoice.CreditCardFeeAmount);Assert.Equal(111.48m,invoice.TotalAmount);Assert.Equal(40m,invoice.BalanceDue);Assert.Equal(0,invoice.Payments.Single(x=>x.Method==PaymentMethod.Cash).ProcessingFee);}
    [Fact] public void CashierViewModel_FillsRemainingInvoiceAmountForEveryPaymentMethodAndPreviewsConfiguredCardFee(){using var db=TemporaryDatabase.Create();var (_,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);var vm=new HammerTrackPro2.ViewModels.CashierViewModel(cashier);vm.Lookup(25);Assert.Equal(110m,vm.SuggestedPaymentAmount(PaymentMethod.Card));Assert.Equal(110m,vm.SuggestedPaymentAmount(PaymentMethod.Cash));Assert.Equal(110m,vm.SuggestedPaymentAmount(PaymentMethod.Check));Assert.Equal(10m,vm.CalculateCashChange(110m,120m));Assert.Equal(0m,vm.CalculateCashChange(110m,100m));Assert.Equal("2.65% + $0.15",vm.CardFeeDescription);Assert.Equal(2.80m,vm.CalculateCardFee(PaymentMethod.Card,100));Assert.Equal(0,vm.CalculateCardFee(PaymentMethod.Cash,100));Assert.Equal(0,vm.CalculateCardFee(PaymentMethod.Check,100));vm.AddPayment(PaymentMethod.Card,100,"");Assert.Equal(2.80m,vm.PendingCardFee);Assert.Equal(102.80m,vm.PendingPayments.Single().TotalTender);Assert.Equal(10m,vm.RemainingInvoiceAmount);Assert.Equal(10m,vm.SuggestedPaymentAmount(PaymentMethod.Card));Assert.Equal(10m,vm.SuggestedPaymentAmount(PaymentMethod.Cash));Assert.Equal(10m,vm.SuggestedPaymentAmount(PaymentMethod.Check));}
    [Fact] public void CashierViewModel_PrintPreviewIncludesSelectedCardFeeWithoutPersistingIt(){using var db=TemporaryDatabase.Create();var (_,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);var vm=new HammerTrackPro2.ViewModels.CashierViewModel(cashier);vm.Lookup(25);var preview=vm.CreateInvoicePreview(PaymentMethod.Card,110);Assert.Equal(3.07m,preview.CreditCardFeeAmount);Assert.Equal(113.07m,preview.TotalAmount);Assert.Equal(113.07m,preview.BalanceDue);Assert.Equal(0m,vm.Invoice!.CreditCardFeeAmount);}
    [Fact]
    public void FullCardPayment_RecordsPretaxFeeBalanceAndPrintsFeeInclusivePaidTotal()
    {
        using var database=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.HammerOnly);
        var auction=store.GetCurrentAuction();auction.BuyerPremiumPercent=0m;auction.SalesTaxPercent=7.25m;store.SaveAuction(auction);
        var viewModel=new HammerTrackPro2.ViewModels.CashierViewModel(cashier);viewModel.Lookup(25);
        Assert.Equal(107.25m,viewModel.Invoice!.BalanceDue);
        Assert.Equal("555-0125",viewModel.Invoice.BuyerPhone);Assert.Equal("buyer@example.com",viewModel.Invoice.BuyerEmail);
        viewModel.AddPayment(PaymentMethod.Card,107.25m,"");
        viewModel.RecordPayments();
        Assert.Equal(2.99m,viewModel.Invoice!.CreditCardFeeAmount);
        Assert.Equal(110.24m,viewModel.Invoice.TotalAmount);
        Assert.Equal(110.24m,viewModel.Invoice.AmountPaid);
        Assert.Equal(0m,viewModel.Invoice.BalanceDue);
        Assert.Equal("Paid",viewModel.Invoice.Status);
        Assert.Equal(110.24m,Assert.Single(viewModel.Invoice.Payments).TenderedAmount);
    }
    [Fact]
    public void RefundRetainsCardFeeAndPaidSaleRequiresRefundBeforeAuditedCorrection()
    {
        using var database=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.None);
        var paid=cashier.RecordPayments(25,[new PaymentRequest{Method=PaymentMethod.Card,Amount=110m}]);var payment=Assert.Single(paid.Payments);Assert.Equal(3.07m,payment.ProcessingFee);
        var sale=Assert.Single(paid.Purchases);var correction=new SaleCorrectionRequest{SaleId=sale.SaleId,BidderNumber=null,Reason="Clerk assigned item to wrong bidder"};
        Assert.Contains("refund",Assert.Throws<InvalidOperationException>(()=>cashier.CorrectSale(25,correction)).Message,StringComparison.OrdinalIgnoreCase);
        var refunded=cashier.RecordRefund(25,new RefundRequest{PaymentId=payment.PaymentId,Amount=110m,Reason="Returned purchase"});
        Assert.Equal(3.07m,refunded.AmountPaid);Assert.Equal(3.07m,refunded.CreditCardFeeAmount);Assert.Equal(110m,Assert.Single(refunded.Refunds).Amount);
        var corrected=cashier.CorrectSale(25,correction);Assert.Empty(corrected.Purchases);Assert.Equal(3.07m,corrected.TotalAmount);Assert.Equal(0m,corrected.BalanceDue);
        Assert.Equal("1",Scalar(database.Path,"SELECT IsVoided FROM Sales WHERE Id=(SELECT MIN(Id) FROM Sales);"));
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM AuditLog WHERE Action='RefundRecorded';"));Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM AuditLog WHERE Action='SaleCorrected';"));
    }

    [Fact]
    public void SaleCorrectionCanMoveUnpaidPurchaseToAnotherRegisteredBidder()
    {
        using var database=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.None);cashier.RegisterBuyer(new AuctionBidder{BidderNumber=26,FirstName="Correct"});var original=cashier.LoadInvoice(25);var sale=Assert.Single(original.Purchases);
        var oldInvoice=cashier.CorrectSale(25,new SaleCorrectionRequest{SaleId=sale.SaleId,BidderNumber=26,Quantity=2,HammerPrice=25m,Reason="Wrong bidder selected"});Assert.Empty(oldInvoice.Purchases);
        var replacement=cashier.LoadInvoice(26);var line=Assert.Single(replacement.Purchases);Assert.Equal(2,line.Quantity);Assert.Equal(25m,line.HammerPrice);Assert.Equal(55m,replacement.TotalAmount);
    }
    [Fact] public void LinkedConsignorBuyer_UnpaidPurchasesReducePayout(){using var db=TemporaryDatabase.Create();var store=new SqliteDataStore(db.Path);var auction=store.GetCurrentAuction();var cs=new ConsignorService(store);cs.SaveConsignor(new Consignor{ConsignorNumber=70,BusinessName="Buyer Seller",Active=true},null);var linked=store.GetConsignors().Single(x=>x.ConsignorNumber==70);Assert.NotNull(linked.LinkedBuyerId);var owner=store.GetConsignors().First();store.SaveLot(new AuctionLot{AuctionId=auction.AuctionId,ConsignorId=owner.ConsignorId,LotNumber=1,Description="Purchase"},null);store.RecordSale(1,70,100);store.SaveLot(new AuctionLot{AuctionId=auction.AuctionId,ConsignorId=linked.ConsignorId,LotNumber=2,Description="Consigned"},null);store.SaveBidder(new AuctionBidder{AuctionId=auction.AuctionId,BidderNumber=71,FirstName="Other"},null);store.RecordSale(2,71,200);var settlement=new CashierService(store).LoadConsignorSettlement(70);Assert.Equal(100,settlement.BuyerPurchaseBalanceDeduction);Assert.Equal(100,settlement.NetAmount);}
    [Fact] public void UnpaidReview_UsesFreshCurrentAuctionBalances(){using var db=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(db.Path,SalesTaxBasis.None);Assert.Single(cashier.GetUnpaidInvoices());cashier.RecordPayments(25,[new PaymentRequest{Method=PaymentMethod.Cash,Amount=110}]);Assert.Empty(cashier.GetUnpaidInvoices());}
    [Fact] public void RegisteredBidderList_IsCurrentAuctionAndNumberSorted(){using var db=TemporaryDatabase.Create();var store=new SqliteDataStore(db.Path);var cashier=new CashierService(store);cashier.RegisterBuyer(new AuctionBidder{BidderNumber=20,FirstName="Twenty"});cashier.RegisterBuyer(new AuctionBidder{BidderNumber=5,FirstName="Five"});Assert.Equal([5,20],cashier.GetRegisteredBidders().Select(x=>x.BuyerNumber));}
    [Fact] public void RegisterBuyer_PersistsEveryOptionalIdentityAndContactField(){using var db=TemporaryDatabase.Create();var store=new SqliteDataStore(db.Path);var cashier=new CashierService(store);cashier.RegisterBuyer(new AuctionBidder{BidderNumber=63,Address="63 Main Street",City="York",State="PA",Zip="17401",Phone="555-0163",Email="bidder63@example.com",DriversLicense="PA-6300"});var buyer=store.Load().Bidders.Single(x=>x.BidderNumber==63);Assert.Equal("63 Main Street",buyer.Address);Assert.Equal("York",buyer.City);Assert.Equal("PA",buyer.State);Assert.Equal("17401",buyer.Zip);Assert.Equal("555-0163",buyer.Phone);Assert.Equal("bidder63@example.com",buyer.Email);Assert.Equal("PA-6300",buyer.DriversLicense);}
    [Fact]
    public void CashierRegistration_CreatesAndSelectsCurrentAuctionRecords()
    {using var db=TemporaryDatabase.Create();var store=new SqliteDataStore(db.Path);var cashier=new CashierService(store);Assert.Equal(41,cashier.RegisterBuyer(new AuctionBidder{BidderNumber=41,FirstName="New",LastName="Buyer"}).BidderNumber);Assert.Equal(42,cashier.RegisterConsignor(new Consignor{ConsignorNumber=42,BusinessName="Farm"}).ConsignorNumber);Assert.Throws<InvalidOperationException>(()=>cashier.RegisterBuyer(new AuctionBidder{BidderNumber=41,FirstName="Dup"}));Assert.Throws<InvalidOperationException>(()=>cashier.RegisterConsignor(new Consignor{ConsignorNumber=42,BusinessName="Dup"}));}

    [Fact]
    public void CashierSellerRegistration_ReusesExistingSameNumberBuyerIdentity()
    {
        using var database=TemporaryDatabase.Create();var store=new SqliteDataStore(database.Path);var cashier=new CashierService(store);
        cashier.RegisterBuyer(new AuctionBidder{BidderNumber=64,FirstName="Jordan",LastName="Seller"});
        var settlement=cashier.RegisterConsignor(new Consignor{ConsignorNumber=64,FirstName="Jordan",LastName="Seller"});
        var bidder=store.Load().Bidders.Single(x=>x.BidderNumber==64);
        var seller=store.GetConsignors().Single(x=>x.ConsignorNumber==64);
        Assert.Equal(bidder.BuyerId,seller.LinkedBuyerId);
        Assert.Equal(64,settlement.ConsignorNumber);
        Assert.Single(store.GetBuyers(),x=>x.BuyerId==seller.LinkedBuyerId);
    }

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
    public void SellerSettlement_UsesCommissionPrecedence_AndAllowsAuditedEarlyPayout()
    {
        using var database=TemporaryDatabase.Create(); var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.None);
        var consignor=store.GetConsignors().First(); consignor.DefaultCommissionPercent=12; consignor.CommissionOverridePercent=8; store.SaveConsignor(consignor,consignor.ConsignorNumber);
        var preview=cashier.LoadConsignorSettlement(consignor.ConsignorNumber);
        Assert.Equal(100m,preview.HammerSubtotal); Assert.Equal(8m,preview.CommissionAmount); Assert.Equal(92m,preview.NetAmount);
        var paid=cashier.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Cash,Amount=20 });
        Assert.Equal("Partially Paid",paid.Status); Assert.Equal(72m,paid.BalanceDue);
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM ConsignorPayouts;"));
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM AuditLog WHERE Action='ConsignorPayoutRecorded';"));
    }

    [Fact]
    public void NewSeller_CreatesMatchingBuyerNumber_AndRejectsExistingBuyerConflictWithoutSavingSeller()
    {
        using var database=TemporaryDatabase.Create(); var store=new SqliteDataStore(database.Path); var auction=store.GetCurrentAuction();
        var sellers=new ConsignorService(store);
        sellers.SaveConsignor(new Consignor { ConsignorNumber=81,BusinessName="Linked Farm",Active=true },null);
        var seller=store.GetConsignors().Single(x=>x.ConsignorNumber==81);
        Assert.NotNull(seller.LinkedBuyerId);
        Assert.Contains(store.Load().Bidders,x=>x.BidderNumber==81&&x.BuyerId==seller.LinkedBuyerId);

        store.SaveBidder(new AuctionBidder { AuctionId=auction.AuctionId,BidderNumber=82,FirstName="Existing",LastName="Buyer" },null);
        var error=Assert.Throws<InvalidOperationException>(()=>sellers.SaveConsignor(new Consignor { ConsignorNumber=82,BusinessName="Conflict Farm",Active=true },null));
        Assert.Contains("already registered to another buyer",error.Message);
        Assert.DoesNotContain(store.GetConsignors(),x=>x.ConsignorNumber==82);

        var renumberedSeller=store.GetConsignors().Single(x=>x.ConsignorNumber==81);
        renumberedSeller.ConsignorNumber=83;
        var linkedError=Assert.Throws<InvalidOperationException>(()=>sellers.SaveConsignor(renumberedSeller,81));
        Assert.Contains("already linked to Buyer Number #81",linkedError.Message);
        Assert.Contains(store.GetConsignors(),x=>x.ConsignorNumber==81);
        Assert.DoesNotContain(store.GetConsignors(),x=>x.ConsignorNumber==83);
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

    [Fact]
    public void Quantity_MultipliesInvoicesSellerTotalsDashboardAndEndOfNight()
    {
        using var database=TemporaryDatabase.Create();var (store,cashier)=CreateAccount(database.Path,SalesTaxBasis.None);var auction=store.GetCurrentAuction();auction.BuyerPremiumPercent=0;store.SaveAuction(auction);var consignor=store.GetConsignors().First();
        store.SaveLot(new AuctionLot{AuctionId=auction.AuctionId,ConsignorId=consignor.ConsignorId,LotNumber=2,Description="Car"},null);store.RecordSale(2,25,5m,false,6);
        var invoice=cashier.LoadInvoice(25);var line=invoice.Purchases.Single(x=>x.LotNumber==2);
        Assert.Equal(6,line.Quantity);Assert.Equal(5m,line.HammerPrice);Assert.Equal(30m,line.LineTotal);Assert.Equal(130m,invoice.HammerSubtotal);
        Assert.Equal(130m,cashier.LoadConsignorSettlement(consignor.ConsignorNumber).HammerSubtotal);Assert.Equal(130m,store.GetDashboardSnapshot().GrossSales);Assert.Equal(130m,cashier.GetAuctionSettlementReport().HammerSales);
    }

    private static (SqliteDataStore Store,CashierService Cashier) CreateAccount(string path,SalesTaxBasis basis)
    {
        var store=new SqliteDataStore(path); var auction=store.GetCurrentAuction();
        auction.AuctionNumber="A-001"; auction.AuctionName="Test Auction"; auction.BuyerPremiumPercent=10; auction.SalesTaxPercent=5; auction.SalesTaxBasis=basis;
        store.SaveAuction(auction);
        store.SaveBidder(new AuctionBidder { AuctionId=auction.AuctionId,BidderNumber=25,FirstName="Alex",LastName="Buyer",Phone="555-0125",Email="buyer@example.com" },null);
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
