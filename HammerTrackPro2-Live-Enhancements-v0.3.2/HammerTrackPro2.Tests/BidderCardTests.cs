using System.Drawing;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Tests;

public sealed class BidderCardTests
{
    [Fact] public void ViewModel_UsesCurrentAuctionBidderListAndValidatesSelection(){var viewModel=new BidderCardViewModel(new BidderCardCashierService());Assert.Equal([12,501],viewModel.RegisteredBidders.Select(x=>x.BuyerNumber));Assert.Equal(12,viewModel.SelectedBidderNumber);viewModel.SelectedBidderNumber=501;Assert.Equal("Sample Bidder",viewModel.GetSelectedBidder().Name);viewModel.SelectedBidderNumber=999;Assert.Contains("current auction",Assert.Throws<InvalidOperationException>(()=>viewModel.GetSelectedBidder()).Message);}
    [Fact] public void Printer_RendersCompleteNumberOnLandscapeCardAndUsesCompanyFallback(){using var bitmap=new Bitmap(1100,850);using var graphics=Graphics.FromImage(bitmap);var bounds=new Rectangle(35,35,1030,780);BidderCardPrinter.Draw(graphics,bounds,913,"Debbie's Auction");var area=new RectangleF(69,35,962,515);using var font=BidderCardPrinter.CreateFittedFont(graphics,"913",area,280,FontStyle.Bold);var measured=graphics.MeasureString("913",font,int.MaxValue,StringFormat.GenericTypographic);Assert.True(measured.Width<=area.Width*0.90F);using var document=new BidderCardPrinter().CreateDocument(913,new Auction{AuctionName="Fallback Auction"});Assert.Equal("Bidder Card 913",document.DocumentName);Assert.True(document.DefaultPageSettings.Landscape);}
    private sealed class BidderCardCashierService : ICashierService
    {
        public Auction GetCurrentAuction()=>new(){AuctionNumber="1",AuctionName="Auction",CompanyName="Debbie's Auction"};public IReadOnlyList<RegisteredBidderSummary> GetRegisteredBidders()=>[new(){BuyerNumber=501,Name="Sample Bidder"},new(){BuyerNumber=12,Name="First Bidder"}];public Invoice LoadInvoice(int bidderNumber)=>throw new NotSupportedException();public Invoice RecordPayments(int bidderNumber,IReadOnlyList<PaymentRequest> payments)=>throw new NotSupportedException();public Invoice RegisterBuyer(AuctionBidder bidder)=>throw new NotSupportedException();public ConsignorSettlement LoadConsignorSettlement(int consignorNumber)=>throw new NotSupportedException();public ConsignorSettlement RecordConsignorPayout(int consignorNumber,ConsignorPayoutRequest payout)=>throw new NotSupportedException();public ConsignorSettlement RegisterConsignor(Consignor consignor)=>throw new NotSupportedException();public IReadOnlyList<UnpaidInvoiceSummary> GetUnpaidInvoices()=>[];public AuctionSettlementReport GetAuctionSettlementReport()=>throw new NotSupportedException();public StaffPayment RecordStaffPayment(StaffPayment payment)=>throw new NotSupportedException();public StaffPayment VoidStaffPayment(long staffPaymentId,string reason,StaffPayment? replacement)=>throw new NotSupportedException();public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken=default)=>Task.FromResult(true);
    }
}
