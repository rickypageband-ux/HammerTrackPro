using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

namespace HammerTrackPro2.Tests;

public sealed class AuctionSetupViewModelTests
{
    [Fact]
    public void SaveDuplicate_CopiesConfigurationAndAssociationsOnlyWhenRequested()
    {
        var service = new FakeAuctionSetupService();
        var viewModel = new AuctionSetupViewModel(service);
        viewModel.Load();

        viewModel.DuplicatePreviousAuction();
        Assert.Equal(0, viewModel.CurrentAuction.AuctionId);
        Assert.Equal(string.Empty, viewModel.CurrentAuction.AuctionNumber);
        Assert.Equal(12.5m, viewModel.CurrentAuction.BuyerPremiumPercent);
        viewModel.CurrentAuction.AuctionNumber = "FALL-26";
        viewModel.CopyConsignors = true;
        viewModel.Save();

        Assert.Equal((1L, 2L), service.CopiedAssociations);
        Assert.Equal(2, service.Auctions.Count);
    }

    [Fact]
    public void NewAuction_DoesNotInheritCommercialTerms()
    {
        var viewModel = new AuctionSetupViewModel(new FakeAuctionSetupService());
        viewModel.Load();
        viewModel.NewAuction();
        Assert.Equal(0, viewModel.CurrentAuction.BuyerPremiumPercent);
        Assert.Equal("Setup", viewModel.CurrentAuction.Status);
    }

    private sealed class FakeAuctionSetupService : IAuctionSetupService
    {
        private long _currentId = 1;
        public List<Auction> Auctions { get; } = [new Auction
        {
            AuctionId = 1, AuctionNumber = "SPRING-26", AuctionName = "Spring", BuyerPremiumPercent = 12.5m,
            DefaultConsignorCommissionPercent = 18, SalesTaxPercent = 6, InvoiceFooter = "Thank you", Status = "Setup"
        }];
        public (long Source, long Destination)? CopiedAssociations { get; private set; }
        IReadOnlyList<Auction> IAuctionSetupService.GetAuctions() => Auctions;
        public Auction GetCurrentAuction() => Auctions.Single(item => item.AuctionId == _currentId);
        public Auction SaveAuction(Auction auction)
        {
            if (auction.AuctionId == 0) { auction.AuctionId = Auctions.Max(item => item.AuctionId) + 1; Auctions.Add(auction); }
            return auction;
        }
        public void SelectAuction(long auctionId) => _currentId = auctionId;
        public void CloseAuction(long auctionId) => GetCurrentAuction().Status = "Closed";
        public void CopyConsignorAssociations(long sourceAuctionId, long destinationAuctionId) => CopiedAssociations = (sourceAuctionId, destinationAuctionId);
        public void ReopenAuction(long auctionId) => throw new NotSupportedException();
    }
}
