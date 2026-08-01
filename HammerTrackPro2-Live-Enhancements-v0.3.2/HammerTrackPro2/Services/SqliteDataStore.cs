using System.Globalization;
using System.Text.Json;
using HammerTrackPro2.Models;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Services;

public sealed class SqliteDataStore : IDataStore, IConsignorRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataFolder;
    private readonly string _databaseFile;
    private readonly string _legacyJsonFile;
    private readonly string _connectionString;
    private readonly object _sync = new();

    public SqliteDataStore(string databaseFile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFile);
        _databaseFile = Path.GetFullPath(databaseFile);
        _dataFolder = Path.GetDirectoryName(_databaseFile)
            ?? throw new InvalidOperationException("The database folder could not be determined.");
        _legacyJsonFile = Path.Combine(_dataFolder, "hammertrack-data.json");
        Directory.CreateDirectory(_dataFolder);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databaseFile,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true,
            DefaultTimeout = 15
        }.ToString();

        InitializeDatabase();
        ImportLegacyJsonWhenNeeded();
    }

    public AppData Load()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            return new AppData
            {
                Bidders = LoadBidders(connection),
                Lots = LoadLots(connection)
            };
        }
    }

    public void SaveBidder(Bidder bidder, int? originalBidderNumber)
    {
        ArgumentNullException.ThrowIfNull(bidder);

        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            if (originalBidderNumber.HasValue && originalBidderNumber.Value != bidder.BidderNumber)
            {
                using var deleteOld = connection.CreateCommand();
                deleteOld.Transaction = transaction;
                deleteOld.CommandText = "DELETE FROM Bidders WHERE BidderNumber = $original;";
                deleteOld.Parameters.AddWithValue("$original", originalBidderNumber.Value);
                deleteOld.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Bidders
                (BidderNumber, FirstName, LastName, DriversLicense, Address, City, State, Zip, Phone, Email, Notes, CreatedUtc, UpdatedUtc)
                VALUES
                ($number, $first, $last, $license, $address, $city, $state, $zip, $phone, $email, $notes, $created, $updated)
                ON CONFLICT(BidderNumber) DO UPDATE SET
                    FirstName = excluded.FirstName,
                    LastName = excluded.LastName,
                    DriversLicense = excluded.DriversLicense,
                    Address = excluded.Address,
                    City = excluded.City,
                    State = excluded.State,
                    Zip = excluded.Zip,
                    Phone = excluded.Phone,
                    Email = excluded.Email,
                    Notes = excluded.Notes,
                    UpdatedUtc = excluded.UpdatedUtc;
                """;
            AddBidderParameters(command, bidder);
            command.ExecuteNonQuery();
            WriteAudit(connection, transaction, "BidderSaved", $"Bidder #{bidder.BidderNumber}: {bidder.FirstName} {bidder.LastName}".Trim());
            transaction.Commit();
        }
    }

    public void DeleteBidder(int bidderNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Bidders WHERE BidderNumber = $number;";
            command.Parameters.AddWithValue("$number", bidderNumber);
            command.ExecuteNonQuery();
            WriteAudit(connection, null, "BidderDeleted", $"Bidder #{bidderNumber}");
        }
    }

    public void SaveLot(Lot lot, int? originalLotNumber)
    {
        ArgumentNullException.ThrowIfNull(lot);

        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            if (originalLotNumber.HasValue && originalLotNumber.Value != lot.LotNumber)
            {
                using var deleteOld = connection.CreateCommand();
                deleteOld.Transaction = transaction;
                deleteOld.CommandText = "DELETE FROM Lots WHERE LotNumber = $original;";
                deleteOld.Parameters.AddWithValue("$original", originalLotNumber.Value);
                deleteOld.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Lots
                (LotNumber, Description, Category, StartingBid, Status, AuctionId, ConsignorId, ReservePrice, PassOutMode, LotStatus, CreatedUtc, UpdatedUtc)
                VALUES
                ($number, $description, $category, $startingBid, $status, $auctionId, $consignorId, $reservePrice, $passOutMode, $lotStatus, $created, $updated)
                ON CONFLICT(LotNumber) DO UPDATE SET
                    Description = excluded.Description,
                    Category = excluded.Category,
                    StartingBid = excluded.StartingBid,
                    Status = excluded.Status,
                    AuctionId = excluded.AuctionId, ConsignorId = excluded.ConsignorId, ReservePrice = excluded.ReservePrice,
                    PassOutMode = excluded.PassOutMode, LotStatus = excluded.LotStatus,
                    UpdatedUtc = excluded.UpdatedUtc;
                """;
            AddLotParameters(command, lot);
            command.ExecuteNonQuery();
            WriteAudit(connection, transaction, "LotSaved", $"Lot #{lot.LotNumber}: {lot.Description}");
            transaction.Commit();
        }
    }

    public void DeleteLot(int lotNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Lots WHERE LotNumber = $number;";
            command.Parameters.AddWithValue("$number", lotNumber);
            command.ExecuteNonQuery();
            WriteAudit(connection, null, "LotDeleted", $"Lot #{lotNumber}");
        }
    }

    public Auction GetCurrentAuction()
    {
        lock (_sync)
        {
            using var connection = OpenConnection(); using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT a.AuctionId, a.AuctionName, a.AuctionDate, a.Location, a.BuyerPremiumPercent,
                       a.DefaultConsignorCommissionPercent, a.SalesTaxPercent, a.Status
                FROM Auctions a LEFT JOIN Settings s ON s.Key='CurrentAuctionId'
                ORDER BY CASE WHEN a.AuctionId=CAST(s.Value AS INTEGER) THEN 0 ELSE 1 END, a.AuctionDate DESC, a.AuctionId DESC LIMIT 1;
                """;
            using var reader = command.ExecuteReader(); if (!reader.Read()) throw new InvalidOperationException("No auction is configured.");
            return new Auction { AuctionId=reader.GetInt64(0), AuctionName=reader.GetString(1), AuctionDate=DateTime.Parse(reader.GetString(2), CultureInfo.InvariantCulture), Location=reader.GetString(3), BuyerPremiumPercent=decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture), DefaultConsignorCommissionPercent=decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture), SalesTaxPercent=decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture), Status=reader.GetString(7) };
        }
    }

    public void SaveAuction(Auction auction)
    {
        lock (_sync)
        {
            using var connection=OpenConnection(); using var transaction=connection.BeginTransaction(); using var command=connection.CreateCommand(); command.Transaction=transaction;
            command.CommandText="""
                UPDATE Auctions SET AuctionName=$name, AuctionDate=$date, Location=$location, BuyerPremiumPercent=$premium,
                    DefaultConsignorCommissionPercent=$commission, SalesTaxPercent=$tax, Status=$status, UpdatedUtc=$updated WHERE AuctionId=$id;
                """;
            command.Parameters.AddWithValue("$id", auction.AuctionId); command.Parameters.AddWithValue("$name", auction.AuctionName); command.Parameters.AddWithValue("$date", auction.AuctionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$location", auction.Location);
            command.Parameters.AddWithValue("$premium", auction.BuyerPremiumPercent.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$commission", auction.DefaultConsignorCommissionPercent.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$tax", auction.SalesTaxPercent.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$status", auction.Status); command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            if(command.ExecuteNonQuery()!=1) throw new InvalidOperationException("The auction was not found."); WriteAudit(connection, transaction, "AuctionSaved", $"Auction #{auction.AuctionId}: {auction.AuctionName}"); transaction.Commit();
        }
    }

    public IReadOnlyList<Consignor> GetConsignors()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT ConsignorId, ConsignorNumber, BusinessName, FirstName, LastName, Address, City, State, Zip,
                       Phone, Email, TaxId, DefaultCommissionPercent, CommissionOverridePercent, Notes, Active,
                       PrintCheck, HoldCheck, TaxExempt, Is1099Required
                FROM Consignors ORDER BY ConsignorNumber;
                """;
            using var reader = command.ExecuteReader();
            var consignors = new List<Consignor>();
            while (reader.Read())
            {
                consignors.Add(new Consignor
                {
                    ConsignorId = reader.GetInt64(0), ConsignorNumber = reader.GetInt32(1), BusinessName = reader.GetString(2),
                    FirstName = reader.GetString(3), LastName = reader.GetString(4), Address = reader.GetString(5), City = reader.GetString(6),
                    State = reader.GetString(7), Zip = reader.GetString(8), Phone = reader.GetString(9), Email = reader.GetString(10),
                    TaxId = reader.GetString(11), DefaultCommissionPercent = decimal.Parse(reader.GetString(12), CultureInfo.InvariantCulture),
                    CommissionOverridePercent = decimal.Parse(reader.GetString(13), CultureInfo.InvariantCulture), Notes = reader.GetString(14), Active = reader.GetInt32(15) != 0,
                    PrintCheck = reader.GetInt32(16) != 0, HoldCheck = reader.GetInt32(17) != 0, TaxExempt = reader.GetInt32(18) != 0, Is1099Required = reader.GetInt32(19) != 0
                });
            }
            return consignors;
        }
    }

    public void SaveConsignor(Consignor consignor, int? originalConsignorNumber)
    {
        ArgumentNullException.ThrowIfNull(consignor);
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            if (originalConsignorNumber.HasValue && originalConsignorNumber.Value != consignor.ConsignorNumber)
            {
                using var renumber = connection.CreateCommand();
                renumber.Transaction = transaction;
                renumber.CommandText = "UPDATE Consignors SET ConsignorNumber = $number, UpdatedUtc = $updated WHERE ConsignorNumber = $original;";
                renumber.Parameters.AddWithValue("$number", consignor.ConsignorNumber);
                renumber.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                renumber.Parameters.AddWithValue("$original", originalConsignorNumber.Value);
                if (renumber.ExecuteNonQuery() != 1) throw new InvalidOperationException("The consignor to update was not found.");
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Consignors
                (ConsignorNumber, BusinessName, FirstName, LastName, Address, City, State, Zip, Phone, Email, TaxId,
                 DefaultCommissionPercent, CommissionOverridePercent, Notes, Active, PrintCheck, HoldCheck, TaxExempt, Is1099Required, CreatedUtc, UpdatedUtc)
                VALUES
                ($number, $business, $first, $last, $address, $city, $state, $zip, $phone, $email, $taxId,
                 $defaultCommission, $overrideCommission, $notes, $active, $printCheck, $holdCheck, $taxExempt, $required1099, $created, $updated)
                ON CONFLICT(ConsignorNumber) DO UPDATE SET
                    BusinessName = excluded.BusinessName,
                    FirstName = excluded.FirstName,
                    LastName = excluded.LastName,
                    Address = excluded.Address,
                    City = excluded.City, State = excluded.State, Zip = excluded.Zip,
                    Phone = excluded.Phone,
                    Email = excluded.Email,
                    TaxId = excluded.TaxId,
                    DefaultCommissionPercent = excluded.DefaultCommissionPercent,
                    CommissionOverridePercent = excluded.CommissionOverridePercent, Notes = excluded.Notes, Active = excluded.Active,
                    PrintCheck = excluded.PrintCheck, HoldCheck = excluded.HoldCheck, TaxExempt = excluded.TaxExempt, Is1099Required = excluded.Is1099Required,
                    UpdatedUtc = excluded.UpdatedUtc;
                """;
            AddConsignorParameters(command, consignor);
            command.ExecuteNonQuery();
            WriteAudit(connection, transaction, "ConsignorSaved", $"Consignor #{consignor.ConsignorNumber}: {consignor.DisplayName}");
            transaction.Commit();
        }
    }

    public void DeleteConsignor(int consignorNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "DELETE FROM Consignors WHERE ConsignorNumber = $number;";
            command.Parameters.AddWithValue("$number", consignorNumber);
            if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Consignor #{consignorNumber} was not found.");
            WriteAudit(connection, transaction, "ConsignorDeleted", $"Consignor #{consignorNumber}");
            transaction.Commit();
        }
    }



    public Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            using (var verify = connection.CreateCommand())
            {
                verify.Transaction = transaction;
                verify.CommandText = "SELECT Status FROM Lots WHERE LotNumber = $lot LIMIT 1;";
                verify.Parameters.AddWithValue("$lot", lotNumber);
                var status = verify.ExecuteScalar()?.ToString();
                if (status is null) throw new InvalidOperationException($"Lot #{lotNumber} was not found.");
                if (string.Equals(status, "Sold", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Lot #{lotNumber} is closed and has already been sold.");
            }

            using (var bidderCheck = connection.CreateCommand())
            {
                bidderCheck.Transaction = transaction;
                bidderCheck.CommandText = "SELECT COUNT(*) FROM Bidders WHERE BidderNumber = $bidder;";
                bidderCheck.Parameters.AddWithValue("$bidder", bidderNumber);
                if (Convert.ToInt32(bidderCheck.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                    throw new InvalidOperationException($"Bidder #{bidderNumber} was not found.");
            }

            var saleUtc = DateTime.UtcNow;
            long saleId;
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO Sales(LotNumber, BidderNumber, HammerPrice, SaleUtc, Paid)
                    VALUES ($lot, $bidder, $price, $saleUtc, 0);
                    SELECT last_insert_rowid();
                    """;
                insert.Parameters.AddWithValue("$lot", lotNumber);
                insert.Parameters.AddWithValue("$bidder", bidderNumber);
                insert.Parameters.AddWithValue("$price", hammerPrice.ToString(CultureInfo.InvariantCulture));
                insert.Parameters.AddWithValue("$saleUtc", saleUtc.ToString("O", CultureInfo.InvariantCulture));
                saleId = Convert.ToInt64(insert.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using (var updateLot = connection.CreateCommand())
            {
                updateLot.Transaction = transaction;
                updateLot.CommandText = "UPDATE Lots SET Status = $status, LotStatus = $status, PassOutMode = $passOut, UpdatedUtc = $updated WHERE LotNumber = $lot;";
                updateLot.Parameters.AddWithValue("$status", keepLotOpen ? "Pass-Out" : "Sold");
                updateLot.Parameters.AddWithValue("$passOut", keepLotOpen ? 1 : 0);
                updateLot.Parameters.AddWithValue("$updated", saleUtc.ToString("O", CultureInfo.InvariantCulture));
                updateLot.Parameters.AddWithValue("$lot", lotNumber);
                if (updateLot.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("The lot could not be marked sold.");
            }

            WriteAudit(connection, transaction, "SaleRecorded", $"Lot #{lotNumber}, bidder #{bidderNumber}, {hammerPrice:C2}, pass-out={keepLotOpen}");
            transaction.Commit();
            return new Sale
            {
                Id = saleId,
                LotNumber = lotNumber,
                BidderNumber = bidderNumber,
                HammerPrice = hammerPrice,
                SaleUtc = saleUtc,
                Paid = false
            };
        }
    }

    public void CloseLot(int lotNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Lots SET Status = 'Sold', LotStatus = 'Sold', PassOutMode = 0, UpdatedUtc = $updated WHERE LotNumber = $lot;";
            command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$lot", lotNumber);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Lot #{lotNumber} was not found.");
            WriteAudit(connection, null, "LotClosed", $"Lot #{lotNumber}");
        }
    }

    public Sale? GetLastSale()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Id, LotNumber, BidderNumber, HammerPrice, SaleUtc, Paid
                FROM Sales
                ORDER BY Id DESC
                LIMIT 1;
                """;
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return ReadSale(reader);
        }
    }

    public void UndoLastSale(long saleId)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            int lotNumber;
            using (var getSale = connection.CreateCommand())
            {
                getSale.Transaction = transaction;
                getSale.CommandText = "SELECT LotNumber FROM Sales WHERE Id = $id LIMIT 1;";
                getSale.Parameters.AddWithValue("$id", saleId);
                var value = getSale.ExecuteScalar();
                if (value is null) throw new InvalidOperationException("The sale could not be found.");
                lotNumber = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }

            using (var ensureLast = connection.CreateCommand())
            {
                ensureLast.Transaction = transaction;
                ensureLast.CommandText = "SELECT Id FROM Sales ORDER BY Id DESC LIMIT 1;";
                var lastId = Convert.ToInt64(ensureLast.ExecuteScalar(), CultureInfo.InvariantCulture);
                if (lastId != saleId) throw new InvalidOperationException("Only the most recent sale can be undone.");
            }

            using (var delete = connection.CreateCommand())
            {
                delete.Transaction = transaction;
                delete.CommandText = "DELETE FROM Sales WHERE Id = $id;";
                delete.Parameters.AddWithValue("$id", saleId);
                delete.ExecuteNonQuery();
            }

            int remainingSales;
            using (var countSales = connection.CreateCommand())
            {
                countSales.Transaction = transaction;
                countSales.CommandText = "SELECT COUNT(*) FROM Sales WHERE LotNumber = $lot;";
                countSales.Parameters.AddWithValue("$lot", lotNumber);
                remainingSales = Convert.ToInt32(countSales.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using (var updateLot = connection.CreateCommand())
            {
                updateLot.Transaction = transaction;
                updateLot.CommandText = "UPDATE Lots SET Status = $status, LotStatus = $status, PassOutMode = $passOut, UpdatedUtc = $updated WHERE LotNumber = $lot;";
                updateLot.Parameters.AddWithValue("$status", remainingSales > 0 ? "Pass-Out" : "Available");
                updateLot.Parameters.AddWithValue("$passOut", remainingSales > 0 ? 1 : 0);
                updateLot.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                updateLot.Parameters.AddWithValue("$lot", lotNumber);
                updateLot.ExecuteNonQuery();
            }

            WriteAudit(connection, transaction, "SaleUndone", $"Sale #{saleId}, lot #{lotNumber}");
            transaction.Commit();
        }
    }

    public int GetSaleCountForLot(int lotNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Sales WHERE LotNumber = $lot;";
            command.Parameters.AddWithValue("$lot", lotNumber);
            return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }
    }

    public DashboardSnapshot GetDashboardSnapshot()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            var snapshot = new DashboardSnapshot();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT
                        (SELECT COUNT(*) FROM Bidders),
                        (SELECT COUNT(*) FROM Lots),
                        (SELECT COUNT(*) FROM Lots WHERE lower(Status) = 'sold'),
                        COALESCE((SELECT SUM(CAST(HammerPrice AS REAL)) FROM Sales), 0),
                        COALESCE((SELECT AVG(CAST(HammerPrice AS REAL)) FROM Sales), 0);
                    """;
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    snapshot.BidderCount = reader.GetInt32(0);
                    snapshot.LotCount = reader.GetInt32(1);
                    snapshot.SoldCount = reader.GetInt32(2);
                    snapshot.GrossSales = (decimal)reader.GetDouble(3);
                    snapshot.AverageSalePrice = (decimal)reader.GetDouble(4);
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT ActivityText
                    FROM (
                        SELECT 'Bidder #' || BidderNumber || ' registered: ' || trim(FirstName || ' ' || LastName) AS ActivityText,
                               UpdatedUtc AS ActivityUtc
                        FROM Bidders
                        UNION ALL
                        SELECT 'Lot #' || LotNumber || ' saved: ' || Description AS ActivityText,
                               UpdatedUtc AS ActivityUtc
                        FROM Lots
                        UNION ALL
                        SELECT 'Lot #' || LotNumber || ' sold to bidder #' || BidderNumber || ' for $' || HammerPrice AS ActivityText,
                               SaleUtc AS ActivityUtc
                        FROM Sales
                    )
                    ORDER BY ActivityUtc DESC
                    LIMIT 8;
                    """;
                using var reader = command.ExecuteReader();
                while (reader.Read()) snapshot.RecentActivity.Add(reader.GetString(0));
            }

            return snapshot;
        }
    }

    public string GetSetting(string key, string defaultValue = "")
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Value FROM Settings WHERE Key = $key LIMIT 1;";
            command.Parameters.AddWithValue("$key", key);
            return command.ExecuteScalar()?.ToString() ?? defaultValue;
        }
    }

    public void SaveSettings(IReadOnlyDictionary<string, string> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            foreach (var pair in settings)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO Settings(Key, Value) VALUES ($key, $value)
                    ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
                    """;
                command.Parameters.AddWithValue("$key", pair.Key);
                command.Parameters.AddWithValue("$value", pair.Value ?? string.Empty);
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
    }

    public string Backup()
    {
        lock (_sync)
        {
            var backupFolder = Path.Combine(_dataFolder, "Backups");
            Directory.CreateDirectory(backupFolder);
            var destination = Path.Combine(backupFolder, $"hammertrack-{DateTime.Now:yyyyMMdd-HHmmss}.db");

            using var source = OpenConnection();
            using var destinationConnection = new SqliteConnection(
                new SqliteConnectionStringBuilder
                {
                    DataSource = destination,
                    Mode = SqliteOpenMode.ReadWriteCreate
                }.ToString());
            destinationConnection.Open();
            source.BackupDatabase(destinationConnection);
            return destination;
        }
    }

    public string DataFilePath => _databaseFile;

    private void InitializeDatabase()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Bidders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BidderNumber INTEGER NOT NULL UNIQUE,
                    FirstName TEXT NOT NULL DEFAULT '',
                    LastName TEXT NOT NULL DEFAULT '',
                    DriversLicense TEXT NOT NULL DEFAULT '',
                    Address TEXT NOT NULL DEFAULT '',
                    City TEXT NOT NULL DEFAULT '',
                    State TEXT NOT NULL DEFAULT '',
                    Zip TEXT NOT NULL DEFAULT '',
                    Phone TEXT NOT NULL DEFAULT '',
                    Email TEXT NOT NULL DEFAULT '',
                    Notes TEXT NOT NULL DEFAULT '',
                    CreatedUtc TEXT NOT NULL,
                    UpdatedUtc TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_Bidders_Name ON Bidders(LastName, FirstName);
                CREATE INDEX IF NOT EXISTS IX_Bidders_DriversLicense ON Bidders(DriversLicense);
                CREATE INDEX IF NOT EXISTS IX_Bidders_Phone ON Bidders(Phone);

                CREATE TABLE IF NOT EXISTS Lots (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LotNumber INTEGER NOT NULL UNIQUE,
                    Description TEXT NOT NULL DEFAULT '',
                    Category TEXT NOT NULL DEFAULT '',
                    StartingBid TEXT NOT NULL DEFAULT '0',
                    Status TEXT NOT NULL DEFAULT 'Available',
                    CreatedUtc TEXT NOT NULL,
                    UpdatedUtc TEXT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS IX_Lots_Description ON Lots(Description);
                CREATE INDEX IF NOT EXISTS IX_Lots_Category ON Lots(Category);
                CREATE INDEX IF NOT EXISTS IX_Lots_Status ON Lots(Status);

                CREATE TABLE IF NOT EXISTS Sales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LotNumber INTEGER NOT NULL,
                    BidderNumber INTEGER NOT NULL,
                    HammerPrice TEXT NOT NULL DEFAULT '0',
                    SaleUtc TEXT NOT NULL,
                    Paid INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (LotNumber) REFERENCES Lots(LotNumber) ON UPDATE CASCADE ON DELETE RESTRICT,
                    FOREIGN KEY (BidderNumber) REFERENCES Bidders(BidderNumber) ON UPDATE CASCADE ON DELETE RESTRICT
                );

                CREATE INDEX IF NOT EXISTS IX_Sales_BidderNumber ON Sales(BidderNumber);

                CREATE TABLE IF NOT EXISTS AuditLog (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EventUtc TEXT NOT NULL,
                    Action TEXT NOT NULL,
                    Details TEXT NOT NULL DEFAULT ''
                );

                CREATE INDEX IF NOT EXISTS IX_AuditLog_EventUtc ON AuditLog(EventUtc);

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL DEFAULT ''
                );

                INSERT INTO Settings(Key, Value)
                VALUES ('DatabaseSchemaVersion', '3')
                ON CONFLICT(Key) DO NOTHING;
                """;
            command.ExecuteNonQuery();
            UpgradeSalesTableForPassOut(connection);
            ApplyDatabaseMigrations(connection);
        }
    }

    private static void ApplyDatabaseMigrations(SqliteConnection connection)
    {
        using var versionCommand = connection.CreateCommand();
        versionCommand.CommandText = "SELECT Value FROM Settings WHERE Key = 'DatabaseSchemaVersion' LIMIT 1;";
        var versionText = versionCommand.ExecuteScalar()?.ToString();
        var version = int.TryParse(versionText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0;
        if (version < 4)
        {
            using var transaction = connection.BeginTransaction();
            using var migration = connection.CreateCommand();
            migration.Transaction = transaction;
            migration.CommandText = """
            CREATE TABLE IF NOT EXISTS Consignors (
                ConsignorId INTEGER PRIMARY KEY AUTOINCREMENT,
                ConsignorNumber INTEGER NOT NULL UNIQUE,
                BusinessName TEXT NOT NULL DEFAULT '',
                FirstName TEXT NOT NULL DEFAULT '',
                LastName TEXT NOT NULL DEFAULT '',
                Address TEXT NOT NULL DEFAULT '',
                City TEXT NOT NULL DEFAULT '', State TEXT NOT NULL DEFAULT '', Zip TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                TaxId TEXT NOT NULL DEFAULT '',
                DefaultCommissionPercent TEXT NOT NULL DEFAULT '0',
                CommissionOverridePercent TEXT NOT NULL DEFAULT '0',
                Notes TEXT NOT NULL DEFAULT '',
                Active INTEGER NOT NULL DEFAULT 1, PrintCheck INTEGER NOT NULL DEFAULT 1, HoldCheck INTEGER NOT NULL DEFAULT 0,
                TaxExempt INTEGER NOT NULL DEFAULT 0, Is1099Required INTEGER NOT NULL DEFAULT 0,
                CreatedUtc TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Consignors_Name ON Consignors(BusinessName, LastName, FirstName);
            CREATE INDEX IF NOT EXISTS IX_Consignors_Phone ON Consignors(Phone);
            CREATE INDEX IF NOT EXISTS IX_Consignors_Email ON Consignors(Email);
            CREATE INDEX IF NOT EXISTS IX_Consignors_Active ON Consignors(Active);
            INSERT INTO Settings(Key, Value) VALUES ('DatabaseSchemaVersion', '4')
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
            migration.ExecuteNonQuery();
            transaction.Commit();
            version = 4;
        }
        if (version >= 5) return;
        using (var transaction = connection.BeginTransaction())
        using (var migration = connection.CreateCommand())
        {
            migration.Transaction = transaction;
            migration.CommandText = """
                CREATE TABLE Auctions (
                    AuctionId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuctionName TEXT NOT NULL DEFAULT '', AuctionDate TEXT NOT NULL,
                    Location TEXT NOT NULL DEFAULT '', BuyerPremiumPercent TEXT NOT NULL DEFAULT '0',
                    DefaultConsignorCommissionPercent TEXT NOT NULL DEFAULT '0', SalesTaxPercent TEXT NOT NULL DEFAULT '0',
                    Status TEXT NOT NULL DEFAULT 'Setup', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL
                );
                CREATE INDEX IX_Auctions_Date ON Auctions(AuctionDate);
                CREATE INDEX IX_Auctions_Status ON Auctions(Status);
                INSERT INTO Auctions(AuctionName, AuctionDate, Location, BuyerPremiumPercent, DefaultConsignorCommissionPercent, SalesTaxPercent, Status, CreatedUtc, UpdatedUtc)
                SELECT COALESCE((SELECT Value FROM Settings WHERE Key='AuctionName'), 'Legacy Auction'),
                       COALESCE((SELECT Value FROM Settings WHERE Key='AuctionDate'), date('now')), '', '0', '0', '0',
                       COALESCE((SELECT Value FROM Settings WHERE Key='AuctionStatus'), 'Setup'), strftime('%Y-%m-%dT%H:%M:%fZ','now'), strftime('%Y-%m-%dT%H:%M:%fZ','now');
                INSERT INTO Consignors(ConsignorNumber, BusinessName, Notes, Active, CreatedUtc, UpdatedUtc)
                SELECT 1, 'Legacy / Unassigned', 'Created during the version 5 migration for existing lots.', 0,
                       strftime('%Y-%m-%dT%H:%M:%fZ','now'), strftime('%Y-%m-%dT%H:%M:%fZ','now')
                WHERE NOT EXISTS (SELECT 1 FROM Consignors);
                ALTER TABLE Lots ADD COLUMN AuctionId INTEGER REFERENCES Auctions(AuctionId);
                ALTER TABLE Lots ADD COLUMN ConsignorId INTEGER REFERENCES Consignors(ConsignorId);
                ALTER TABLE Lots ADD COLUMN ReservePrice TEXT NOT NULL DEFAULT '0';
                ALTER TABLE Lots ADD COLUMN PassOutMode INTEGER NOT NULL DEFAULT 0;
                ALTER TABLE Lots ADD COLUMN LotStatus TEXT NOT NULL DEFAULT 'Available';
                UPDATE Lots SET AuctionId=(SELECT MIN(AuctionId) FROM Auctions), ConsignorId=(SELECT MIN(ConsignorId) FROM Consignors),
                                LotStatus=Status WHERE AuctionId IS NULL OR ConsignorId IS NULL;
                CREATE INDEX IX_Lots_AuctionId ON Lots(AuctionId);
                CREATE INDEX IX_Lots_ConsignorId ON Lots(ConsignorId);
                CREATE INDEX IX_Lots_AuctionLotNumber ON Lots(AuctionId, LotNumber);
                INSERT INTO Settings(Key, Value) VALUES ('CurrentAuctionId', (SELECT CAST(MIN(AuctionId) AS TEXT) FROM Auctions))
                ON CONFLICT(Key) DO NOTHING;
                INSERT INTO Settings(Key, Value) VALUES ('DatabaseSchemaVersion', '5') ON CONFLICT(Key) DO UPDATE SET Value='5';
                """;
            migration.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    private static void UpgradeSalesTableForPassOut(SqliteConnection connection)
    {
        using var indexCheck = connection.CreateCommand();
        indexCheck.CommandText = "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'Sales';";
        var sql = indexCheck.ExecuteScalar()?.ToString() ?? string.Empty;
        if (!sql.Contains("LotNumber INTEGER NOT NULL UNIQUE", StringComparison.OrdinalIgnoreCase)) return;

        using var transaction = connection.BeginTransaction();
        using var migration = connection.CreateCommand();
        migration.Transaction = transaction;
        migration.CommandText = """
            CREATE TABLE Sales_New (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LotNumber INTEGER NOT NULL,
                BidderNumber INTEGER NOT NULL,
                HammerPrice TEXT NOT NULL DEFAULT '0',
                SaleUtc TEXT NOT NULL,
                Paid INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (LotNumber) REFERENCES Lots(LotNumber) ON UPDATE CASCADE ON DELETE RESTRICT,
                FOREIGN KEY (BidderNumber) REFERENCES Bidders(BidderNumber) ON UPDATE CASCADE ON DELETE RESTRICT
            );
            INSERT INTO Sales_New(Id, LotNumber, BidderNumber, HammerPrice, SaleUtc, Paid)
            SELECT Id, LotNumber, BidderNumber, HammerPrice, SaleUtc, Paid FROM Sales;
            DROP TABLE Sales;
            ALTER TABLE Sales_New RENAME TO Sales;
            CREATE INDEX IF NOT EXISTS IX_Sales_BidderNumber ON Sales(BidderNumber);
            CREATE INDEX IF NOT EXISTS IX_Sales_LotNumber ON Sales(LotNumber);
            INSERT INTO Settings(Key, Value) VALUES ('DatabaseSchemaVersion', '3')
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;
        migration.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void WriteAudit(SqliteConnection connection, SqliteTransaction? transaction, string action, string details)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO AuditLog(EventUtc, Action, Details) VALUES ($utc, $action, $details);";
        command.Parameters.AddWithValue("$utc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$details", details);
        command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA busy_timeout = 15000;";
        command.ExecuteNonQuery();
        return connection;
    }

    private void ImportLegacyJsonWhenNeeded()
    {
        lock (_sync)
        {
            if (!File.Exists(_legacyJsonFile)) return;

            using var connection = OpenConnection();
            using var count = connection.CreateCommand();
            count.CommandText = "SELECT (SELECT COUNT(*) FROM Bidders) + (SELECT COUNT(*) FROM Lots);";
            if (Convert.ToInt64(count.ExecuteScalar(), CultureInfo.InvariantCulture) > 0) return;

            var json = File.ReadAllText(_legacyJsonFile);
            var legacy = JsonSerializer.Deserialize<AppData>(json, JsonOptions);
            if (legacy is null) return;

            using var transaction = connection.BeginTransaction();
            foreach (var bidder in legacy.Bidders)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT OR IGNORE INTO Bidders
                    (BidderNumber, FirstName, LastName, DriversLicense, Address, City, State, Zip, Phone, Email, Notes, CreatedUtc, UpdatedUtc)
                    VALUES
                    ($number, $first, $last, $license, $address, $city, $state, $zip, $phone, $email, $notes, $created, $updated);
                    """;
                AddBidderParameters(command, bidder);
                command.ExecuteNonQuery();
            }

            foreach (var lot in legacy.Lots)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT OR IGNORE INTO Lots
                    (LotNumber, Description, Category, StartingBid, Status, CreatedUtc, UpdatedUtc)
                    VALUES
                    ($number, $description, $category, $startingBid, $status, $created, $updated);
                    """;
                AddLotParameters(command, lot);
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            File.Move(_legacyJsonFile, _legacyJsonFile + ".migrated", true);
        }
    }

    private static List<Bidder> LoadBidders(SqliteConnection connection)
    {
        var bidders = new List<Bidder>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT BidderNumber, FirstName, LastName, DriversLicense, Address, City, State, Zip, Phone, Email, Notes
            FROM Bidders
            ORDER BY BidderNumber;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            bidders.Add(new Bidder
            {
                BidderNumber = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DriversLicense = reader.GetString(3),
                Address = reader.GetString(4),
                City = reader.GetString(5),
                State = reader.GetString(6),
                Zip = reader.GetString(7),
                Phone = reader.GetString(8),
                Email = reader.GetString(9),
                Notes = reader.GetString(10)
            });
        }
        return bidders;
    }

    private static List<Lot> LoadLots(SqliteConnection connection)
    {
        var lots = new List<Lot>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT LotNumber, Description, Category, StartingBid, Status, AuctionId, ConsignorId, ReservePrice, PassOutMode, LotStatus
            FROM Lots
            ORDER BY LotNumber;
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lots.Add(new Lot
            {
                LotNumber = reader.GetInt32(0),
                Description = reader.GetString(1),
                Category = reader.GetString(2),
                StartingBid = decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                Status = reader.GetString(4), AuctionId = reader.IsDBNull(5) ? 0 : reader.GetInt64(5), ConsignorId = reader.IsDBNull(6) ? 0 : reader.GetInt64(6),
                ReservePrice = decimal.Parse(reader.GetString(7), CultureInfo.InvariantCulture), PassOutMode = reader.GetInt32(8) != 0, LotStatus = reader.GetString(9)
            });
        }
        return lots;
    }


    private static Sale ReadSale(SqliteDataReader reader)
    {
        return new Sale
        {
            Id = reader.GetInt64(0),
            LotNumber = reader.GetInt32(1),
            BidderNumber = reader.GetInt32(2),
            HammerPrice = decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
            SaleUtc = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Paid = reader.GetInt32(5) != 0
        };
    }

    private static void AddBidderParameters(SqliteCommand command, Bidder bidder)
    {
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        command.Parameters.AddWithValue("$number", bidder.BidderNumber);
        command.Parameters.AddWithValue("$first", bidder.FirstName ?? string.Empty);
        command.Parameters.AddWithValue("$last", bidder.LastName ?? string.Empty);
        command.Parameters.AddWithValue("$license", bidder.DriversLicense ?? string.Empty);
        command.Parameters.AddWithValue("$address", bidder.Address ?? string.Empty);
        command.Parameters.AddWithValue("$city", bidder.City ?? string.Empty);
        command.Parameters.AddWithValue("$state", bidder.State ?? string.Empty);
        command.Parameters.AddWithValue("$zip", bidder.Zip ?? string.Empty);
        command.Parameters.AddWithValue("$phone", bidder.Phone ?? string.Empty);
        command.Parameters.AddWithValue("$email", bidder.Email ?? string.Empty);
        command.Parameters.AddWithValue("$notes", bidder.Notes ?? string.Empty);
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
    }

    private static void AddLotParameters(SqliteCommand command, Lot lot)
    {
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        command.Parameters.AddWithValue("$number", lot.LotNumber);
        command.Parameters.AddWithValue("$description", lot.Description ?? string.Empty);
        command.Parameters.AddWithValue("$category", lot.Category ?? string.Empty);
        command.Parameters.AddWithValue("$startingBid", lot.StartingBid.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", lot.Status ?? "Available");
        command.Parameters.AddWithValue("$auctionId", lot.AuctionId); command.Parameters.AddWithValue("$consignorId", lot.ConsignorId);
        command.Parameters.AddWithValue("$reservePrice", lot.ReservePrice.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$passOutMode", lot.PassOutMode ? 1 : 0);
        command.Parameters.AddWithValue("$lotStatus", string.IsNullOrWhiteSpace(lot.LotStatus) ? lot.Status : lot.LotStatus);
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
    }

    private static void AddConsignorParameters(SqliteCommand command, Consignor consignor)
    {
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        command.Parameters.AddWithValue("$number", consignor.ConsignorNumber); command.Parameters.AddWithValue("$business", consignor.BusinessName ?? string.Empty);
        command.Parameters.AddWithValue("$first", consignor.FirstName ?? string.Empty); command.Parameters.AddWithValue("$last", consignor.LastName ?? string.Empty);
        command.Parameters.AddWithValue("$address", consignor.Address ?? string.Empty); command.Parameters.AddWithValue("$city", consignor.City ?? string.Empty);
        command.Parameters.AddWithValue("$state", consignor.State ?? string.Empty); command.Parameters.AddWithValue("$zip", consignor.Zip ?? string.Empty);
        command.Parameters.AddWithValue("$phone", consignor.Phone ?? string.Empty); command.Parameters.AddWithValue("$email", consignor.Email ?? string.Empty);
        command.Parameters.AddWithValue("$taxId", consignor.TaxId ?? string.Empty);
        command.Parameters.AddWithValue("$defaultCommission", consignor.DefaultCommissionPercent.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$overrideCommission", consignor.CommissionOverridePercent.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$notes", consignor.Notes ?? string.Empty); command.Parameters.AddWithValue("$active", consignor.Active ? 1 : 0);
        command.Parameters.AddWithValue("$printCheck", consignor.PrintCheck ? 1 : 0); command.Parameters.AddWithValue("$holdCheck", consignor.HoldCheck ? 1 : 0);
        command.Parameters.AddWithValue("$taxExempt", consignor.TaxExempt ? 1 : 0); command.Parameters.AddWithValue("$required1099", consignor.Is1099Required ? 1 : 0);
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
    }
}
