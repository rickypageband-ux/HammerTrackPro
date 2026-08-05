using System.Globalization;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Tests;

public sealed class MigrationTests
{
    [Fact]
    public void ConsignorRepository_PerformsCreateUpdateReadAndDelete()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteDataStore(database.Path);
        var consignor = new Consignor
        {
            ConsignorNumber = 31,
            BusinessName = "North Farm",
            DefaultCommissionPercent = 15,
            Require1099 = true,
            Active = true
        };

        store.SaveConsignor(consignor, null);
        var created = Assert.Single(store.GetConsignors(), item => item.ConsignorNumber == 31);
        created.BusinessName = "North Farm Estate";
        created.HoldCheck = true;
        store.SaveConsignor(created, 31);

        var updated = Assert.Single(store.GetConsignors(), item => item.ConsignorNumber == 31);
        Assert.Equal("North Farm Estate", updated.BusinessName);
        Assert.True(updated.HoldCheck);
        Assert.True(updated.Require1099);

        store.DeleteConsignor(31);
        Assert.DoesNotContain(store.GetConsignors(), item => item.ConsignorNumber == 31);
    }

    [Fact]
    public void NewDatabase_CreatesSprintOneSchemaAndPersistsRelationships()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteDataStore(database.Path);
        var auction = store.GetCurrentAuction();
        store.SaveConsignor(new Consignor
        {
            ConsignorNumber = 22,
            BusinessName = "Oak Hill",
            Require1099 = true,
            CreatedDate = DateTime.UtcNow,
            Active = true
        }, null);
        var consignor = store.GetConsignors().Single(item => item.ConsignorNumber == 22);

        store.SaveLot(new AuctionLot
        {
            AuctionId = auction.AuctionId,
            ConsignorId = consignor.ConsignorId,
            LotNumber = 44,
            Description = "Clock",
            Status = "Available",
            LotStatus = "Available"
        }, null);

        var lot = store.Load().Lots.Single(item => item.LotNumber == 44);
        Assert.Equal(auction.AuctionId, lot.AuctionId);
        Assert.Equal(consignor.ConsignorId, lot.ConsignorId);
        Assert.True(consignor.Require1099);
        Assert.NotEqual(default, consignor.CreatedDate);
        Assert.Equal("14", Scalar(database.Path, "SELECT Value FROM Settings WHERE Key='DatabaseSchemaVersion';"));
        Assert.Equal("IX_Consignors_ConsignorNumber", Scalar(database.Path, "SELECT name FROM sqlite_master WHERE type='index' AND name='IX_Consignors_ConsignorNumber';"));
        Assert.Equal("IX_AuctionLots_AuctionLotNumber", Scalar(database.Path, "SELECT name FROM sqlite_master WHERE type='index' AND name='IX_AuctionLots_AuctionLotNumber';"));
        Assert.Equal("IX_AuctionBidders_AuctionBidderNumber", Scalar(database.Path, "SELECT name FROM sqlite_master WHERE type='index' AND name='IX_AuctionBidders_AuctionBidderNumber';"));
        Assert.Equal("IX_Payments_InvoiceId", Scalar(database.Path, "SELECT name FROM sqlite_master WHERE type='index' AND name='IX_Payments_InvoiceId';"));
        Assert.Equal("IX_ConsignorPayouts_SettlementId", Scalar(database.Path, "SELECT name FROM sqlite_master WHERE type='index' AND name='IX_ConsignorPayouts_SettlementId';"));
    }

    [Fact]
    public void VersionThreeDatabase_UpgradesWithoutLosingExistingLot()
    {
        using var database = TemporaryDatabase.Create();
        CreateVersionThreeDatabase(database.Path);

        var store = new SqliteDataStore(database.Path);
        var lot = Assert.Single(store.Load().Lots);

        Assert.Equal(7, lot.LotNumber);
        Assert.True(lot.AuctionId > 0);
        Assert.True(lot.ConsignorId > 0);
        Assert.Equal("14", Scalar(database.Path, "SELECT Value FROM Settings WHERE Key='DatabaseSchemaVersion';"));
        Assert.Equal("7", Scalar(database.Path, "SELECT LotNumber FROM Lots WHERE LotNumber=7;"));
        Assert.Equal("7", Scalar(database.Path, "SELECT LotNumber FROM AuctionLots WHERE LotNumber=7;"));
    }

    [Fact]
    public void AuctionLots_AllowTheSameLotNumberInDifferentAuctions()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteDataStore(database.Path);
        var first = store.GetCurrentAuction();
        var second = store.SaveAuction(NewAuction("FALL-26", "Fall Auction"));
        var consignorId = long.Parse(Scalar(database.Path, "SELECT MIN(ConsignorId) FROM Consignors;")!, CultureInfo.InvariantCulture);

        store.SaveLot(new AuctionLot { AuctionId = first.AuctionId, ConsignorId = consignorId, LotNumber = 100, Description = "First" }, null);
        store.SelectAuction(second.AuctionId);
        store.SaveLot(new AuctionLot { AuctionId = second.AuctionId, ConsignorId = consignorId, LotNumber = 100, Description = "Second" }, null);

        Assert.Equal("2", Scalar(database.Path, "SELECT COUNT(*) FROM AuctionLots WHERE LotNumber=100;"));
        Assert.Equal("Second", Assert.Single(store.Load().Lots).Description);
    }

    [Fact]
    public void CopyConsignors_CopiesAssociationsAndNeverDuplicatesMasterRows()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteDataStore(database.Path);
        var first = store.GetCurrentAuction();
        store.SaveConsignor(new Consignor { ConsignorNumber = 50, BusinessName = "Smith Farm" }, null);
        store.AssociateConsignorWithCurrentAuction(50);
        var second = store.SaveAuction(NewAuction("SUMMER-26", "Summer Auction"));

        store.CopyConsignorAssociations(first.AuctionId, second.AuctionId);

        Assert.Equal("2", Scalar(database.Path, "SELECT COUNT(*) FROM Consignors;"));
        Assert.Equal("2", Scalar(database.Path, "SELECT COUNT(*) FROM AuctionConsignors WHERE ConsignorId=(SELECT ConsignorId FROM Consignors WHERE ConsignorNumber=50);"));
    }

    [Fact]
    public void CloseAuction_WritesAuditAndLocksOperationalChanges()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteDataStore(database.Path);
        var service = new AuctionService(store);
        var auction = store.GetCurrentAuction();

        service.CloseAuction(auction.AuctionId);

        Assert.Equal("Closed", store.GetCurrentAuction().Status);
        Assert.Equal("1", Scalar(database.Path, "SELECT COUNT(*) FROM AuditLog WHERE Action='AuctionClosed';"));
        Assert.Throws<InvalidOperationException>(() => service.SaveBidder(new AuctionBidder { AuctionId=auction.AuctionId, BidderNumber=10, FirstName="Pat" }, null));
        Assert.Throws<NotSupportedException>(() => service.ReopenAuction(auction.AuctionId));
    }

    [Fact]
    public void AuctionBidders_ReuseGlobalBuyerAndAllowNumberInDifferentAuctions()
    {
        using var database = TemporaryDatabase.Create();
        var store = new SqliteDataStore(database.Path);
        var first = store.GetCurrentAuction();
        store.SaveBidder(new AuctionBidder { AuctionId=first.AuctionId,BidderNumber=25,FirstName="John",LastName="Smith" }, null);
        var buyer = Assert.Single(store.GetBuyers());
        var second = store.SaveAuction(NewAuction("SECOND-26", "Second Auction"));
        store.SelectAuction(second.AuctionId);

        store.SaveBidder(new AuctionBidder { AuctionId=second.AuctionId,BuyerId=buyer.BuyerId,BidderNumber=25,FirstName=buyer.FirstName,LastName=buyer.LastName }, null);
        var consignorId = long.Parse(Scalar(database.Path, "SELECT MIN(ConsignorId) FROM Consignors;")!, CultureInfo.InvariantCulture);
        store.SaveLot(new AuctionLot { AuctionId=second.AuctionId,ConsignorId=consignorId,LotNumber=1,Description="Test lot" }, null);
        var sale = store.RecordSale(1, 25, 100m);

        Assert.Equal("1", Scalar(database.Path, "SELECT COUNT(*) FROM Buyers;"));
        Assert.Equal("2", Scalar(database.Path, "SELECT COUNT(*) FROM AuctionBidders WHERE BidderNumber=25;"));
        Assert.Equal(1, store.GetDashboardSnapshot().BidderCount);
        Assert.Single(store.Load().Bidders);
        Assert.Equal(second.AuctionId, sale.AuctionId);
        Assert.Equal("1", Scalar(database.Path, "SELECT COUNT(*) FROM Sales WHERE AuctionId=(SELECT AuctionId FROM Auctions WHERE AuctionNumber='SECOND-26');"));
    }

    private static Auction NewAuction(string number, string name) => new()
    {
        AuctionNumber = number, AuctionName = name, AuctionDate = DateTime.Today, Status = "Setup"
    };

    private static string? Scalar(string path, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar()?.ToString();
    }

    private static void CreateVersionThreeDatabase(string path)
    {
        using var connection = new SqliteConnection($"Data Source={path}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE Bidders (Id INTEGER PRIMARY KEY AUTOINCREMENT, BidderNumber INTEGER NOT NULL UNIQUE,
                FirstName TEXT NOT NULL DEFAULT '', LastName TEXT NOT NULL DEFAULT '', DriversLicense TEXT NOT NULL DEFAULT '',
                Address TEXT NOT NULL DEFAULT '', City TEXT NOT NULL DEFAULT '', State TEXT NOT NULL DEFAULT '', Zip TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '', Email TEXT NOT NULL DEFAULT '', Notes TEXT NOT NULL DEFAULT '', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL);
            CREATE TABLE Lots (Id INTEGER PRIMARY KEY AUTOINCREMENT, LotNumber INTEGER NOT NULL UNIQUE,
                Description TEXT NOT NULL DEFAULT '', Category TEXT NOT NULL DEFAULT '', StartingBid TEXT NOT NULL DEFAULT '0',
                Status TEXT NOT NULL DEFAULT 'Available', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL);
            CREATE TABLE Sales (Id INTEGER PRIMARY KEY AUTOINCREMENT, LotNumber INTEGER NOT NULL, BidderNumber INTEGER NOT NULL,
                HammerPrice TEXT NOT NULL DEFAULT '0', SaleUtc TEXT NOT NULL, Paid INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE AuditLog (Id INTEGER PRIMARY KEY AUTOINCREMENT, EventUtc TEXT NOT NULL, Action TEXT NOT NULL, Details TEXT NOT NULL DEFAULT '');
            CREATE TABLE Settings (Key TEXT PRIMARY KEY, Value TEXT NOT NULL DEFAULT '');
            INSERT INTO Settings(Key, Value) VALUES ('DatabaseSchemaVersion','3'), ('AuctionName','Legacy Event'), ('AuctionDate','2026-07-01');
            INSERT INTO Lots(LotNumber, Description, Category, StartingBid, Status, CreatedUtc, UpdatedUtc)
            VALUES (7, 'Legacy clock', 'Antiques', '25', 'Available', $utc, $utc);
            """;
        command.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _folder;
        private TemporaryDatabase(string folder) { _folder = folder; Path = System.IO.Path.Combine(folder, "test.db"); }
        public string Path { get; }
        public static TemporaryDatabase Create()
        {
            var folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "HammerTrackPro2.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            return new TemporaryDatabase(folder);
        }
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_folder, true);
        }
    }
}
