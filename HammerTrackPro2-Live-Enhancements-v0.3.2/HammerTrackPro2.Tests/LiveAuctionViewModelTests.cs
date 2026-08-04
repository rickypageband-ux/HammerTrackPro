using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Tests;

public sealed class LiveAuctionViewModelTests
{
    [Fact]
    public void QuickAdd_CreatesCurrentAuctionLotAndAssociatesActiveConsignor()
    {
        using var database=TemporaryDatabase.Create();
        var store=new SqliteDataStore(database.Path);
        var service=new AuctionService(store);
        var consignors=new ConsignorService(store);
        consignors.SaveConsignor(new Consignor { ConsignorNumber=77,BusinessName="Quick Farm",Active=true },null);
        var existing=consignors.GetByNumber(1);
        service.SaveLot(new AuctionLot { AuctionId=service.GetCurrentAuction().AuctionId,ConsignorId=existing.ConsignorId,LotNumber=8,Description="Existing" },null);

        var lot=new LiveAuctionViewModel(service,consignors).QuickAddLot(77,"  Hay wagon  ");

        Assert.Equal(9,lot.LotNumber);
        Assert.Equal("Hay wagon",lot.Description);
        Assert.Equal(service.GetCurrentAuction().AuctionId,lot.AuctionId);
        Assert.Equal(consignors.GetByNumber(77).ConsignorId,lot.ConsignorId);
        Assert.Equal("Available",lot.Status);
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM AuctionConsignors WHERE AuctionId=(SELECT CAST(Value AS INTEGER) FROM Settings WHERE Key='CurrentAuctionId') AND ConsignorId=(SELECT ConsignorId FROM Consignors WHERE ConsignorNumber=77);"));
    }

    [Fact]
    public void QuickAdd_RejectsInactiveOrUnknownConsignor()
    {
        using var database=TemporaryDatabase.Create();
        var store=new SqliteDataStore(database.Path);
        var service=new AuctionService(store);
        var consignors=new ConsignorService(store);
        consignors.SaveConsignor(new Consignor { ConsignorNumber=88,BusinessName="Inactive",Active=false },null);
        var viewModel=new LiveAuctionViewModel(service,consignors);

        Assert.Throws<InvalidOperationException>(()=>viewModel.QuickAddLot(88,"Item"));
        Assert.Throws<InvalidOperationException>(()=>viewModel.QuickAddLot(999,"Item"));
        Assert.Empty(service.Load().Lots);
    }

    [Fact]
    public void PassLot_MarksUnsoldLotPassedAndAuditsButRejectsLotWithBuyerEntries()
    {
        using var database=TemporaryDatabase.Create();
        var store=new SqliteDataStore(database.Path);
        var service=new AuctionService(store);
        var consignors=new ConsignorService(store);
        var viewModel=new LiveAuctionViewModel(service,consignors);
        consignors.SaveConsignor(new Consignor { ConsignorNumber=66,BusinessName="Active Farm",Active=true },null);
        var lot=viewModel.QuickAddLot(66,"Passed item");

        viewModel.PassLot(lot.LotNumber);

        Assert.Equal("Passed",Assert.Single(service.Load().Lots).Status);
        Assert.Equal("1",Scalar(database.Path,"SELECT COUNT(*) FROM AuditLog WHERE Action='LotPassed';"));

        var active=viewModel.QuickAddLot(66,"Sold item");
        service.SaveBidder(new AuctionBidder { AuctionId=service.GetCurrentAuction().AuctionId,BidderNumber=12,FirstName="Buyer" },null);
        service.RecordSale(active.LotNumber,12,25m,true);
        Assert.Throws<InvalidOperationException>(()=>viewModel.PassLot(active.LotNumber));
    }

    private static string? Scalar(string path,string sql)
    {
        using var connection=new SqliteConnection($"Data Source={path}"); connection.Open();
        using var command=connection.CreateCommand(); command.CommandText=sql; return command.ExecuteScalar()?.ToString();
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _folder;
        private TemporaryDatabase(string folder){_folder=folder;Path=System.IO.Path.Combine(folder,"test.db");}
        public string Path { get; }
        public static TemporaryDatabase Create(){var folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"HammerTrackPro2.Tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);return new(folder);}
        public void Dispose(){SqliteConnection.ClearAllPools();Directory.Delete(_folder,true);}
    }
}
