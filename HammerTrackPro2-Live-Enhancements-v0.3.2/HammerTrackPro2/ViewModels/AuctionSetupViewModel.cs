using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class AuctionSetupViewModel
{
    private readonly IAuctionSetupService _service;
    private long? _duplicateSourceAuctionId;

    public AuctionSetupViewModel(IAuctionSetupService service) => _service = service ?? throw new ArgumentNullException(nameof(service));

    public IReadOnlyList<Auction> Auctions { get; private set; } = [];
    public Auction CurrentAuction { get; private set; } = new();
    public bool CopyConsignors { get; set; }

    public void Load()
    {
        Auctions = _service.GetAuctions();
        CurrentAuction = _service.GetCurrentAuction();
        _duplicateSourceAuctionId = null;
        CopyConsignors = false;
    }

    public void NewAuction()
    {
        CurrentAuction = new Auction { AuctionDate = DateTime.Today, Status = "Setup" };
        _duplicateSourceAuctionId = null;
        CopyConsignors = false;
    }

    public void LoadAuction(long auctionId)
    {
        _service.SelectAuction(auctionId);
        Load();
    }

    public Auction Save()
    {
        var saved = _service.SaveAuction(CurrentAuction);
        if (_duplicateSourceAuctionId.HasValue && CopyConsignors)
            _service.CopyConsignorAssociations(_duplicateSourceAuctionId.Value, saved.AuctionId);
        _service.SelectAuction(saved.AuctionId);
        Load();
        return CurrentAuction;
    }

    public void CloseAuction()
    {
        if (CurrentAuction.AuctionId <= 0) throw new InvalidOperationException("Save the auction before closing it.");
        _service.CloseAuction(CurrentAuction.AuctionId);
        Load();
    }

    public void DuplicatePreviousAuction()
    {
        if (CurrentAuction.AuctionId <= 0) throw new InvalidOperationException("Load a saved auction before duplicating it.");
        var source = CurrentAuction;
        _duplicateSourceAuctionId = source.AuctionId;
        CurrentAuction = new Auction
        {
            AuctionName = source.AuctionName, AuctionDate = DateTime.Today, AuctionTime = source.AuctionTime,
            Location = source.Location, Auctioneer = source.Auctioneer, Clerk = source.Clerk, Cashier = source.Cashier,
            BuyerPremiumPercent = source.BuyerPremiumPercent,
            DefaultConsignorCommissionPercent = source.DefaultConsignorCommissionPercent,
            SalesTaxPercent = source.SalesTaxPercent, InvoiceFooter = source.InvoiceFooter, ReceiptFooter = source.ReceiptFooter,
            Status = "Setup"
        };
        CopyConsignors = false;
    }
}
