using System.Drawing;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Tests;

public sealed class BidderCardTests
{
    [Fact] public void ViewModel_UsesCurrentAuctionBidderListAndValidatesSelection(){var viewModel=new BidderCardViewModel(new BidderCardCashierService());Assert.Equal([12,501],viewModel.RegisteredBidders.Select(x=>x.BuyerNumber));Assert.Equal(12,viewModel.SelectedBidderNumber);viewModel.SelectedBidderNumber=501;Assert.Equal("Sample Bidder",viewModel.GetSelectedBidder().Name);viewModel.SelectedBidderNumber=999;Assert.Contains("current auction",Assert.Throws<InvalidOperationException>(()=>viewModel.GetSelectedBidder()).Message);}
    [Fact] public void Printer_RendersFullPageCardAndUsesCompanyFallback(){using var bitmap=new Bitmap(850,1100);using var graphics=Graphics.FromImage(bitmap);BidderCardPrinter.Draw(graphics,new Rectangle(35,35,780,1030),501,"Debbie's Auction");using var font=BidderCardPrinter.CreateFittedFont(graphics,"501",new RectangleF(35,35,780,710),310,FontStyle.Bold);Assert.True(font.Size>=150);using var document=new BidderCardPrinter().CreateDocument(501,new Auction{AuctionName="Fallback Auction"});Assert.Equal("Bidder Card 501",document.DocumentName);Assert.False(document.DefaultPageSettings.Landscape);}
    private sealed class BidderCardCashierService : ICashierService
    {
        public Auction GetCurrentAuction()=>new(){AuctionNumber="1",AuctionName="Auction",CompanyName="Debbie's Auction"};public IReadOnlyList<RegisteredBidderSummary> GetRegisteredBidders()=>[new(){BuyerNumber=501,Name="Sample Bidder"},new(){BuyerNumber=12,Name="First Bidder"}];public Invoice LoadInvoice(int bidderNumber)=>throw new NotSupportedException();public Invoice RecordPayments(int bidderNumber,IReadOnlyList<PaymentRequest> payments)=>throw new NotSupportedException();public Invoice RegisterBuyer(AuctionBidder bidder)=>throw new NotSupportedException();public ConsignorSettlement LoadConsignorSettlement(int consignorNumber)=>throw new NotSupportedException();public ConsignorSettlement RecordConsignorPayout(int consignorNumber,ConsignorPayoutRequest payout)=>throw new NotSupportedException();public ConsignorSettlement RegisterConsignor(Consignor consignor)=>throw new NotSupportedException();public IReadOnlyList<UnpaidInvoiceSummary> GetUnpaidInvoices()=>[];public AuctionSettlementReport GetAuctionSettlementReport()=>throw new NotSupportedException();public StaffPayment RecordStaffPayment(StaffPayment payment)=>throw new NotSupportedException();public StaffPayment VoidStaffPayment(long staffPaymentId,string reason,StaffPayment? replacement)=>throw new NotSupportedException();public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken=default)=>Task.FromResult(true);
    }
}
