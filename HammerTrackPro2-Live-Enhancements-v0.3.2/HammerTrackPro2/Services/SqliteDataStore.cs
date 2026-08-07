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
                Bidders = LoadAuctionBidders(connection, GetCurrentAuctionId(connection)),
                Lots = LoadAuctionLots(connection, GetCurrentAuctionId(connection))
            };
        }
    }

    public IReadOnlyList<Buyer> GetBuyers()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT BuyerId,FirstName,LastName,DriversLicense,Address,City,State,Zip,Phone,Email
                FROM Buyers ORDER BY LastName,FirstName,BuyerId;
                """;
            using var reader = command.ExecuteReader();
            var buyers = new List<Buyer>();
            while (reader.Read()) buyers.Add(new Buyer
            {
                BuyerId=reader.GetInt64(0),FirstName=reader.GetString(1),LastName=reader.GetString(2),DriversLicense=reader.GetString(3),
                Address=reader.GetString(4),City=reader.GetString(5),State=reader.GetString(6),Zip=reader.GetString(7),Phone=reader.GetString(8),Email=reader.GetString(9)
            });
            return buyers;
        }
    }

    public Invoice LoadCashierInvoice(int bidderNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            var auctionId = GetCurrentAuctionId(connection);
            long auctionBidderId;
            using (var registration = connection.CreateCommand())
            {
                registration.CommandText = "SELECT AuctionBidderId FROM AuctionBidders WHERE AuctionId=$auctionId AND BidderNumber=$number LIMIT 1;";
                registration.Parameters.AddWithValue("$auctionId", auctionId); registration.Parameters.AddWithValue("$number", bidderNumber);
                var value = registration.ExecuteScalar();
                if (value is null) throw new InvalidOperationException($"Bidder #{bidderNumber} is not registered in the current auction.");
                auctionBidderId = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
            using (var create = connection.CreateCommand())
            {
                create.CommandText = """
                    INSERT OR IGNORE INTO Invoices(AuctionId,AuctionBidderId,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,
                        HammerSubtotal,BuyerPremiumAmount,SalesTaxAmount,TotalAmount,Status,CreatedUtc,UpdatedUtc)
                    SELECT a.AuctionId,$registration,a.BuyerPremiumPercent,a.SalesTaxPercent,a.SalesTaxBasis,
                        '0','0','0','0','Unpaid',$now,$now FROM Auctions a WHERE a.AuctionId=$auctionId;
                    """;
                create.Parameters.AddWithValue("$registration", auctionBidderId); create.Parameters.AddWithValue("$auctionId", auctionId);
                create.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)); create.ExecuteNonQuery();
            }
            using (var terms=connection.CreateCommand())
            {
                terms.CommandText="""
                    UPDATE Invoices SET BuyerPremiumPercent=(SELECT BuyerPremiumPercent FROM Auctions WHERE AuctionId=$auctionId),
                        SalesTaxPercent=(SELECT SalesTaxPercent FROM Auctions WHERE AuctionId=$auctionId),
                        SalesTaxBasis=(SELECT SalesTaxBasis FROM Auctions WHERE AuctionId=$auctionId)
                    WHERE AuctionBidderId=$registration AND NOT EXISTS(SELECT 1 FROM Payments WHERE Payments.InvoiceId=Invoices.InvoiceId);
                    """;
                terms.Parameters.AddWithValue("$auctionId",auctionId); terms.Parameters.AddWithValue("$registration",auctionBidderId); terms.ExecuteNonQuery();
            }

            Invoice result;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT i.InvoiceId,i.AuctionId,i.AuctionBidderId,a.AuctionNumber,ab.BidderNumber,
                           trim(b.FirstName || ' ' || b.LastName),trim(b.Address || ' ' || b.City || ' ' || b.State || ' ' || b.Zip),b.Phone,b.Email,
                           i.BuyerPremiumPercent,i.SalesTaxPercent,i.SalesTaxBasis,i.HammerSubtotal,i.BuyerPremiumAmount,
                           i.SalesTaxAmount,i.TotalAmount,i.Status,i.CreatedUtc,i.UpdatedUtc,i.CreditCardFeeAmount
                    FROM Invoices i JOIN Auctions a ON a.AuctionId=i.AuctionId
                    JOIN AuctionBidders ab ON ab.AuctionBidderId=i.AuctionBidderId JOIN Buyers b ON b.BuyerId=ab.BuyerId
                    WHERE i.AuctionBidderId=$registration LIMIT 1;
                    """;
                command.Parameters.AddWithValue("$registration", auctionBidderId);
                using var reader = command.ExecuteReader(); reader.Read();
                result = new Invoice
                {
                    InvoiceId=reader.GetInt64(0),AuctionId=reader.GetInt64(1),AuctionBidderId=reader.GetInt64(2),AuctionNumber=reader.GetString(3),
                    BidderNumber=reader.GetInt32(4),BuyerName=reader.GetString(5),BuyerAddress=reader.GetString(6),BuyerPhone=reader.GetString(7),BuyerEmail=reader.GetString(8),
                    BuyerPremiumPercent=decimal.Parse(reader.GetString(9),CultureInfo.InvariantCulture),SalesTaxPercent=decimal.Parse(reader.GetString(10),CultureInfo.InvariantCulture),
                    SalesTaxBasis=Enum.Parse<SalesTaxBasis>(reader.GetString(11)),HammerSubtotal=decimal.Parse(reader.GetString(12),CultureInfo.InvariantCulture),
                    BuyerPremiumAmount=decimal.Parse(reader.GetString(13),CultureInfo.InvariantCulture),SalesTaxAmount=decimal.Parse(reader.GetString(14),CultureInfo.InvariantCulture),
                    TotalAmount=decimal.Parse(reader.GetString(15),CultureInfo.InvariantCulture),Status=reader.GetString(16),
                    CreatedUtc=DateTime.Parse(reader.GetString(17),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),
                    UpdatedUtc=DateTime.Parse(reader.GetString(18),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),CreditCardFeeAmount=decimal.Parse(reader.GetString(19),CultureInfo.InvariantCulture)
                };
            }
            using (var purchases = connection.CreateCommand())
            {
                purchases.CommandText = """
                    SELECT s.Id,s.LotNumber,al.Description,s.Quantity,s.HammerPrice FROM Sales s
                    JOIN AuctionLots al ON al.AuctionLotId=s.AuctionLotId WHERE s.AuctionBidderId=$registration AND s.IsVoided=0 ORDER BY s.Id;
                    """;
                purchases.Parameters.AddWithValue("$registration", auctionBidderId); using var reader = purchases.ExecuteReader();
                while (reader.Read()) result.Purchases.Add(new InvoiceLine { SaleId=reader.GetInt64(0),LotNumber=reader.GetInt32(1),Description=reader.GetString(2),Quantity=reader.GetInt32(3),HammerPrice=decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture) });
            }
            using (var payments = connection.CreateCommand())
            {
                payments.CommandText = "SELECT PaymentId,InvoiceId,Method,TenderedAmount,AppliedAmount,ChangeDue,Reference,CreatedUtc,ProcessingFee FROM Payments WHERE InvoiceId=$invoice ORDER BY PaymentId;";
                payments.Parameters.AddWithValue("$invoice", result.InvoiceId); using var reader = payments.ExecuteReader();
                while (reader.Read()) result.Payments.Add(new Payment
                {
                    PaymentId=reader.GetInt64(0),InvoiceId=reader.GetInt64(1),Method=Enum.Parse<PaymentMethod>(reader.GetString(2)),
                    TenderedAmount=decimal.Parse(reader.GetString(3),CultureInfo.InvariantCulture),AppliedAmount=decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture),
                    ChangeDue=decimal.Parse(reader.GetString(5),CultureInfo.InvariantCulture),Reference=reader.GetString(6),
                    CreatedUtc=DateTime.Parse(reader.GetString(7),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),ProcessingFee=decimal.Parse(reader.GetString(8),CultureInfo.InvariantCulture)
                });
            }
            using(var refunds=connection.CreateCommand())
            {
                refunds.CommandText="SELECT RefundId,InvoiceId,PaymentId,Method,Amount,Reason,Reference,CreatedUtc FROM Refunds WHERE InvoiceId=$invoice ORDER BY RefundId;";refunds.Parameters.AddWithValue("$invoice",result.InvoiceId);using var reader=refunds.ExecuteReader();while(reader.Read())result.Refunds.Add(new Refund{RefundId=reader.GetInt64(0),InvoiceId=reader.GetInt64(1),PaymentId=reader.GetInt64(2),Method=Enum.Parse<PaymentMethod>(reader.GetString(3)),Amount=decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture),Reason=reader.GetString(5),Reference=reader.GetString(6),CreatedUtc=DateTime.Parse(reader.GetString(7),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind)});
            }
            return result;
        }
    }

    public Invoice RecordInvoiceRefund(int bidderNumber,Refund refund)
    {
        lock(_sync){using var c=OpenConnection();using var t=c.BeginTransaction();var now=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture);using(var q=c.CreateCommand()){q.Transaction=t;q.CommandText="SELECT p.InvoiceId,p.Method,CAST(p.AppliedAmount AS REAL)-CAST(p.ProcessingFee AS REAL)-COALESCE((SELECT SUM(CAST(r.Amount AS REAL)) FROM Refunds r WHERE r.PaymentId=p.PaymentId),0) FROM Payments p JOIN Invoices i ON i.InvoiceId=p.InvoiceId JOIN AuctionBidders ab ON ab.AuctionBidderId=i.AuctionBidderId WHERE p.PaymentId=$payment AND ab.AuctionId=$auction AND ab.BidderNumber=$bidder;";q.Parameters.AddWithValue("$payment",refund.PaymentId);q.Parameters.AddWithValue("$auction",GetCurrentAuctionId(c));q.Parameters.AddWithValue("$bidder",bidderNumber);using var r=q.ExecuteReader();if(!r.Read())throw new InvalidOperationException("The selected payment was not found for this bidder.");var remaining=Convert.ToDecimal(r.GetDouble(2),CultureInfo.InvariantCulture);if(refund.Amount>remaining)throw new InvalidOperationException($"Refund no more than the remaining purchase amount of {remaining:C2}.");refund.InvoiceId=r.GetInt64(0);refund.Method=Enum.Parse<PaymentMethod>(r.GetString(1));}
            using(var insert=c.CreateCommand()){insert.Transaction=t;insert.CommandText="INSERT INTO Refunds(InvoiceId,PaymentId,Method,Amount,Reason,Reference,CreatedUtc) VALUES($invoice,$payment,$method,$amount,$reason,$reference,$utc); SELECT last_insert_rowid();";insert.Parameters.AddWithValue("$invoice",refund.InvoiceId);insert.Parameters.AddWithValue("$payment",refund.PaymentId);insert.Parameters.AddWithValue("$method",refund.Method.ToString());insert.Parameters.AddWithValue("$amount",refund.Amount.ToString(CultureInfo.InvariantCulture));insert.Parameters.AddWithValue("$reason",refund.Reason);insert.Parameters.AddWithValue("$reference",refund.Reference);insert.Parameters.AddWithValue("$utc",now);refund.RefundId=Convert.ToInt64(insert.ExecuteScalar(),CultureInfo.InvariantCulture);}
            using(var update=c.CreateCommand()){update.Transaction=t;update.CommandText="UPDATE Invoices SET Status=CASE WHEN (SELECT COALESCE(SUM(CAST(AppliedAmount AS REAL)),0) FROM Payments WHERE InvoiceId=$invoice)-(SELECT COALESCE(SUM(CAST(Amount AS REAL)),0) FROM Refunds WHERE InvoiceId=$invoice)>=CAST(TotalAmount AS REAL) THEN 'Paid' WHEN (SELECT COALESCE(SUM(CAST(AppliedAmount AS REAL)),0) FROM Payments WHERE InvoiceId=$invoice)-(SELECT COALESCE(SUM(CAST(Amount AS REAL)),0) FROM Refunds WHERE InvoiceId=$invoice)>0 THEN 'Partially Paid' ELSE 'Unpaid' END,UpdatedUtc=$utc WHERE InvoiceId=$invoice; UPDATE Sales SET Paid=0 WHERE AuctionBidderId=(SELECT AuctionBidderId FROM Invoices WHERE InvoiceId=$invoice);";update.Parameters.AddWithValue("$invoice",refund.InvoiceId);update.Parameters.AddWithValue("$utc",now);update.ExecuteNonQuery();}
            WriteAudit(c,t,"RefundRecorded",$"Invoice #{refund.InvoiceId}, payment #{refund.PaymentId}, method={refund.Method}, amount={refund.Amount:C2}, reason={refund.Reason}");t.Commit();return LoadCashierInvoice(bidderNumber);}
    }

    public Invoice CorrectInvoiceSale(int bidderNumber,SaleCorrectionRequest correction)
    {
        lock(_sync){using var c=OpenConnection();using var t=c.BeginTransaction();var auctionId=GetCurrentAuctionId(c);long lotId;int lotNumber;long oldRegistration;decimal oldTotal;using(var q=c.CreateCommand()){q.Transaction=t;q.CommandText="SELECT AuctionLotId,LotNumber,AuctionBidderId,CAST(HammerPrice AS REAL)*Quantity FROM Sales WHERE Id=$id AND AuctionId=$auction AND IsVoided=0;";q.Parameters.AddWithValue("$id",correction.SaleId);q.Parameters.AddWithValue("$auction",auctionId);using var r=q.ExecuteReader();if(!r.Read())throw new InvalidOperationException("The selected sale was not found or was already corrected.");lotId=r.GetInt64(0);lotNumber=r.GetInt32(1);oldRegistration=r.GetInt64(2);oldTotal=Convert.ToDecimal(r.GetDouble(3),CultureInfo.InvariantCulture);}
            long? newRegistration=null;if(correction.BidderNumber is int newBidder){using var q=c.CreateCommand();q.Transaction=t;q.CommandText="SELECT AuctionBidderId FROM AuctionBidders WHERE AuctionId=$auction AND BidderNumber=$bidder;";q.Parameters.AddWithValue("$auction",auctionId);q.Parameters.AddWithValue("$bidder",newBidder);var value=q.ExecuteScalar();if(value is null)throw new InvalidOperationException($"Bidder #{newBidder} is not registered in the current auction.");newRegistration=Convert.ToInt64(value,CultureInfo.InvariantCulture);}
            var projectedReduction=oldTotal-(correction.BidderNumber is null?0m:correction.Quantity*correction.HammerPrice);if(projectedReduction>0){using var q=c.CreateCommand();q.Transaction=t;q.CommandText="SELECT COALESCE(SUM(CAST(p.AppliedAmount AS REAL)),0)-COALESCE((SELECT SUM(CAST(r.Amount AS REAL)) FROM Refunds r WHERE r.InvoiceId=i.InvoiceId),0),CAST(i.TotalAmount AS REAL) FROM Invoices i LEFT JOIN Payments p ON p.InvoiceId=i.InvoiceId WHERE i.AuctionBidderId=$registration GROUP BY i.InvoiceId;";q.Parameters.AddWithValue("$registration",oldRegistration);using var r=q.ExecuteReader();if(r.Read()){var netPaid=Convert.ToDecimal(r.GetDouble(0),CultureInfo.InvariantCulture);var currentTotal=Convert.ToDecimal(r.GetDouble(1),CultureInfo.InvariantCulture);if(netPaid>currentTotal-projectedReduction)throw new InvalidOperationException($"Record a refund of at least {netPaid-(currentTotal-projectedReduction):C2} before reducing or removing this paid sale.");}}
            var utc=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture);using(var update=c.CreateCommand()){update.Transaction=t;update.CommandText="UPDATE Sales SET IsVoided=1,VoidedUtc=$utc,VoidReason=$reason WHERE Id=$id;";update.Parameters.AddWithValue("$utc",utc);update.Parameters.AddWithValue("$reason",correction.Reason);update.Parameters.AddWithValue("$id",correction.SaleId);update.ExecuteNonQuery();}
            long? replacementId=null;if(correction.BidderNumber is int replacementBidder){using var insert=c.CreateCommand();insert.Transaction=t;insert.CommandText="INSERT INTO Sales(AuctionId,AuctionLotId,AuctionBidderId,LotNumber,BidderNumber,HammerPrice,Quantity,SaleUtc,Paid,CorrectionForSaleId) VALUES($auction,$lot,$registration,$lotNumber,$bidder,$price,$quantity,$utc,0,$original); SELECT last_insert_rowid();";insert.Parameters.AddWithValue("$auction",auctionId);insert.Parameters.AddWithValue("$lot",lotId);insert.Parameters.AddWithValue("$registration",newRegistration!.Value);insert.Parameters.AddWithValue("$lotNumber",lotNumber);insert.Parameters.AddWithValue("$bidder",replacementBidder);insert.Parameters.AddWithValue("$price",correction.HammerPrice.ToString(CultureInfo.InvariantCulture));insert.Parameters.AddWithValue("$quantity",correction.Quantity);insert.Parameters.AddWithValue("$utc",utc);insert.Parameters.AddWithValue("$original",correction.SaleId);replacementId=Convert.ToInt64(insert.ExecuteScalar(),CultureInfo.InvariantCulture);}else{using var lot=c.CreateCommand();lot.Transaction=t;lot.CommandText="UPDATE AuctionLots SET Status='Available',LotStatus='Available',UpdatedUtc=$utc WHERE AuctionLotId=$lot AND NOT EXISTS(SELECT 1 FROM Sales WHERE AuctionLotId=$lot AND IsVoided=0);";lot.Parameters.AddWithValue("$utc",utc);lot.Parameters.AddWithValue("$lot",lotId);lot.ExecuteNonQuery();}
            RefreshInvoiceTotals(c,t,oldRegistration);if(newRegistration is long nr&&nr!=oldRegistration)RefreshInvoiceTotals(c,t,nr);WriteAudit(c,t,"SaleCorrected",$"Sale #{correction.SaleId}, lot #{lotNumber}, replacement={replacementId?.ToString()??"removed"}, bidder={correction.BidderNumber?.ToString()??"none"}, quantity={correction.Quantity}, unit={correction.HammerPrice:C2}, reason={correction.Reason}");t.Commit();return LoadCashierInvoice(bidderNumber);}
    }

    public Invoice RecordInvoicePayments(Invoice invoice, IReadOnlyList<Payment> payments)
    {
        lock (_sync)
        {
            using var connection = OpenConnection(); using var transaction = connection.BeginTransaction();
            var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            using (var balance=connection.CreateCommand())
            {
                balance.Transaction=transaction;
                balance.CommandText="SELECT COALESCE(SUM(CAST(AppliedAmount AS REAL)),0) FROM Payments WHERE InvoiceId=$invoice;";
                balance.Parameters.AddWithValue("$invoice",invoice.InvoiceId);
                var existing=Convert.ToDecimal(balance.ExecuteScalar(),CultureInfo.InvariantCulture);
                if(existing+payments.Sum(item=>item.AppliedAmount)>invoice.TotalAmount)
                    throw new InvalidOperationException("The invoice balance changed. Reload it before recording payments.");
            }
            foreach (var payment in payments)
            {
                using var command = connection.CreateCommand(); command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO Payments(InvoiceId,Method,TenderedAmount,AppliedAmount,ChangeDue,Reference,CreatedUtc,ProcessingFee)
                    VALUES($invoice,$method,$tendered,$applied,$change,$reference,$created,$fee);
                    """;
                command.Parameters.AddWithValue("$invoice", invoice.InvoiceId); command.Parameters.AddWithValue("$method", payment.Method.ToString());
                command.Parameters.AddWithValue("$tendered", payment.TenderedAmount.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$applied", payment.AppliedAmount.ToString(CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$change", payment.ChangeDue.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$reference", payment.Reference);
                command.Parameters.AddWithValue("$created", now);command.Parameters.AddWithValue("$fee",payment.ProcessingFee.ToString(CultureInfo.InvariantCulture)); command.ExecuteNonQuery();
            }
            using (var update = connection.CreateCommand())
            {
                update.Transaction=transaction; update.CommandText="""
                    UPDATE Invoices SET HammerSubtotal=$hammer,BuyerPremiumAmount=$premium,SalesTaxAmount=$tax,CreditCardFeeAmount=$fee,
                        TotalAmount=$total,Status=$status,UpdatedUtc=$updated WHERE InvoiceId=$id;
                    """;
                update.Parameters.AddWithValue("$hammer",invoice.HammerSubtotal.ToString(CultureInfo.InvariantCulture)); update.Parameters.AddWithValue("$premium",invoice.BuyerPremiumAmount.ToString(CultureInfo.InvariantCulture));
                update.Parameters.AddWithValue("$tax",invoice.SalesTaxAmount.ToString(CultureInfo.InvariantCulture)); update.Parameters.AddWithValue("$total",invoice.TotalAmount.ToString(CultureInfo.InvariantCulture));
                update.Parameters.AddWithValue("$fee",invoice.CreditCardFeeAmount.ToString(CultureInfo.InvariantCulture));
                update.Parameters.AddWithValue("$status",invoice.Status); update.Parameters.AddWithValue("$updated",now); update.Parameters.AddWithValue("$id",invoice.InvoiceId); update.ExecuteNonQuery();
            }
            using (var sales = connection.CreateCommand())
            {
                sales.Transaction=transaction; sales.CommandText="UPDATE Sales SET Paid=$paid WHERE AuctionBidderId=$registration;";
                sales.Parameters.AddWithValue("$paid",invoice.Status == "Paid" ? 1 : 0); sales.Parameters.AddWithValue("$registration",invoice.AuctionBidderId); sales.ExecuteNonQuery();
            }
            WriteAudit(connection,transaction,"PaymentRecorded",$"Invoice #{invoice.InvoiceId}, payments={payments.Count}, status={invoice.Status}");
            transaction.Commit(); return LoadCashierInvoice(invoice.BidderNumber);
        }
    }

    public ConsignorSettlement LoadConsignorSettlement(int consignorNumber)
    {
        lock(_sync)
        {
            using var connection=OpenConnection();
            var auctionId=GetCurrentAuctionId(connection);
            long consignorId;
            using(var find=connection.CreateCommand())
            {
                find.CommandText="""
                    SELECT c.ConsignorId FROM Consignors c
                    JOIN AuctionConsignors ac ON ac.ConsignorId=c.ConsignorId
                    WHERE ac.AuctionId=$auction AND c.ConsignorNumber=$number LIMIT 1;
                    """;
                find.Parameters.AddWithValue("$auction",auctionId); find.Parameters.AddWithValue("$number",consignorNumber);
                var value=find.ExecuteScalar();
                if(value is null) throw new InvalidOperationException($"Seller #{consignorNumber} is not associated with the current auction.");
                consignorId=Convert.ToInt64(value,CultureInfo.InvariantCulture);
            }
            var now=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture);
            using(var create=connection.CreateCommand())
            {
                create.CommandText="""
                    INSERT OR IGNORE INTO ConsignorSettlements(AuctionId,ConsignorId,CommissionPercent,HammerSubtotal,CommissionAmount,NetAmount,Status,CreatedUtc,UpdatedUtc)
                    SELECT $auction,$consignor,
                        CASE WHEN CAST(c.CommissionOverridePercent AS REAL)>0 THEN c.CommissionOverridePercent
                             WHEN CAST(c.DefaultCommissionPercent AS REAL)>0 THEN c.DefaultCommissionPercent
                             ELSE a.DefaultConsignorCommissionPercent END,
                        '0','0','0','Unpaid',$now,$now
                    FROM Consignors c CROSS JOIN Auctions a WHERE c.ConsignorId=$consignor AND a.AuctionId=$auction;
                    UPDATE ConsignorSettlements SET CommissionPercent=(
                        SELECT CASE WHEN CAST(c.CommissionOverridePercent AS REAL)>0 THEN c.CommissionOverridePercent
                                    WHEN CAST(c.DefaultCommissionPercent AS REAL)>0 THEN c.DefaultCommissionPercent
                                    ELSE a.DefaultConsignorCommissionPercent END
                        FROM Consignors c CROSS JOIN Auctions a WHERE c.ConsignorId=$consignor AND a.AuctionId=$auction),UpdatedUtc=$now
                    WHERE AuctionId=$auction AND ConsignorId=$consignor
                      AND NOT EXISTS(SELECT 1 FROM ConsignorPayouts p WHERE p.ConsignorSettlementId=ConsignorSettlements.ConsignorSettlementId);
                    """;
                create.Parameters.AddWithValue("$auction",auctionId); create.Parameters.AddWithValue("$consignor",consignorId); create.Parameters.AddWithValue("$now",now); create.ExecuteNonQuery();
            }
            ConsignorSettlement result;
            using(var command=connection.CreateCommand())
            {
                command.CommandText="""
                    SELECT cs.ConsignorSettlementId,cs.AuctionId,cs.ConsignorId,a.AuctionNumber,c.ConsignorNumber,
                        CASE WHEN c.BusinessName<>'' THEN c.BusinessName ELSE trim(c.FirstName||' '||c.LastName) END,
                        trim(c.Address||' '||c.City||' '||c.State||' '||c.Zip),cs.CommissionPercent,
                        cs.HammerSubtotal,cs.CommissionAmount,cs.NetAmount,cs.Status,cs.CreatedUtc,cs.UpdatedUtc,c.HoldCheck
                    FROM ConsignorSettlements cs JOIN Auctions a ON a.AuctionId=cs.AuctionId JOIN Consignors c ON c.ConsignorId=cs.ConsignorId
                    WHERE cs.AuctionId=$auction AND cs.ConsignorId=$consignor;
                    """;
                command.Parameters.AddWithValue("$auction",auctionId); command.Parameters.AddWithValue("$consignor",consignorId);
                using var reader=command.ExecuteReader(); reader.Read();
                result=new ConsignorSettlement
                {
                    ConsignorSettlementId=reader.GetInt64(0),AuctionId=reader.GetInt64(1),ConsignorId=reader.GetInt64(2),AuctionNumber=reader.GetString(3),
                    ConsignorNumber=reader.GetInt32(4),ConsignorName=reader.GetString(5),ConsignorAddress=reader.GetString(6),
                    CommissionPercent=decimal.Parse(reader.GetString(7),CultureInfo.InvariantCulture),HammerSubtotal=decimal.Parse(reader.GetString(8),CultureInfo.InvariantCulture),
                    CommissionAmount=decimal.Parse(reader.GetString(9),CultureInfo.InvariantCulture),NetAmount=decimal.Parse(reader.GetString(10),CultureInfo.InvariantCulture),Status=reader.GetString(11),
                    CreatedUtc=DateTime.Parse(reader.GetString(12),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),UpdatedUtc=DateTime.Parse(reader.GetString(13),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),HoldCheck=reader.GetInt32(14)!=0
                };
            }
            using(var lines=connection.CreateCommand())
            {
                lines.CommandText="""
                    SELECT s.Id,al.LotNumber,al.Description,s.Quantity,s.HammerPrice FROM Sales s
                    JOIN AuctionLots al ON al.AuctionLotId=s.AuctionLotId
                    WHERE s.AuctionId=$auction AND al.ConsignorId=$consignor AND s.IsVoided=0 ORDER BY al.LotNumber,s.Id;
                    """;
                lines.Parameters.AddWithValue("$auction",auctionId); lines.Parameters.AddWithValue("$consignor",consignorId);
                using var reader=lines.ExecuteReader(); while(reader.Read()) result.Lots.Add(new ConsignorSettlementLine
                { SaleId=reader.GetInt64(0),LotNumber=reader.GetInt32(1),Description=reader.GetString(2),Quantity=reader.GetInt32(3),HammerPrice=decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture) });
            }
            using(var payouts=connection.CreateCommand())
            {
                payouts.CommandText="SELECT ConsignorPayoutId,ConsignorSettlementId,Method,Amount,Reference,CreatedUtc FROM ConsignorPayouts WHERE ConsignorSettlementId=$id ORDER BY ConsignorPayoutId;";
                payouts.Parameters.AddWithValue("$id",result.ConsignorSettlementId); using var reader=payouts.ExecuteReader();
                while(reader.Read()) result.Payouts.Add(new ConsignorPayout
                { ConsignorPayoutId=reader.GetInt64(0),ConsignorSettlementId=reader.GetInt64(1),Method=Enum.Parse<ConsignorPayoutMethod>(reader.GetString(2)),Amount=decimal.Parse(reader.GetString(3),CultureInfo.InvariantCulture),Reference=reader.GetString(4),CreatedUtc=DateTime.Parse(reader.GetString(5),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind) });
            }
            using(var deduction=connection.CreateCommand())
            {
                deduction.CommandText="SELECT COALESCE(CAST(i.TotalAmount AS REAL)-COALESCE((SELECT SUM(CAST(p.AppliedAmount AS REAL)) FROM Payments p WHERE p.InvoiceId=i.InvoiceId),0),0) FROM Consignors c JOIN AuctionBidders ab ON ab.BuyerId=c.LinkedBuyerId AND ab.AuctionId=$auction JOIN Invoices i ON i.AuctionBidderId=ab.AuctionBidderId WHERE c.ConsignorId=$consignor LIMIT 1;";
                deduction.Parameters.AddWithValue("$auction",auctionId);deduction.Parameters.AddWithValue("$consignor",consignorId);result.BuyerPurchaseBalanceDeduction=Math.Max(0,Convert.ToDecimal(deduction.ExecuteScalar()??0,CultureInfo.InvariantCulture));
            }
            return result;
        }
    }

    public ConsignorSettlement RecordConsignorPayout(ConsignorSettlement settlement,ConsignorPayout payout)
    {
        lock(_sync)
        {
            using var connection=OpenConnection(); using var transaction=connection.BeginTransaction();
            var now=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture);
            using(var guard=connection.CreateCommand())
            {
                guard.Transaction=transaction; guard.CommandText="SELECT Amount FROM ConsignorPayouts WHERE ConsignorSettlementId=$id;";
                guard.Parameters.AddWithValue("$id",settlement.ConsignorSettlementId);
                using var reader=guard.ExecuteReader(); var paid=0m;
                while(reader.Read()) paid+=decimal.Parse(reader.GetString(0),CultureInfo.InvariantCulture);
                if(paid+payout.Amount>settlement.NetAmount) throw new InvalidOperationException("The settlement balance changed. Reload it before recording the payout.");
            }
            using(var insert=connection.CreateCommand())
            {
                insert.Transaction=transaction; insert.CommandText="INSERT INTO ConsignorPayouts(ConsignorSettlementId,Method,Amount,Reference,CreatedUtc) VALUES($id,$method,$amount,$reference,$now);";
                insert.Parameters.AddWithValue("$id",settlement.ConsignorSettlementId); insert.Parameters.AddWithValue("$method",payout.Method.ToString()); insert.Parameters.AddWithValue("$amount",payout.Amount.ToString(CultureInfo.InvariantCulture)); insert.Parameters.AddWithValue("$reference",payout.Reference); insert.Parameters.AddWithValue("$now",now); insert.ExecuteNonQuery();
            }
            var totalPaid=settlement.AmountPaid+payout.Amount; var status=totalPaid>=settlement.NetAmount?"Paid":"Partially Paid";
            using(var update=connection.CreateCommand())
            {
                update.Transaction=transaction; update.CommandText="UPDATE ConsignorSettlements SET HammerSubtotal=$hammer,CommissionAmount=$commission,NetAmount=$net,Status=$status,UpdatedUtc=$now WHERE ConsignorSettlementId=$id;";
                update.Parameters.AddWithValue("$hammer",settlement.HammerSubtotal.ToString(CultureInfo.InvariantCulture)); update.Parameters.AddWithValue("$commission",settlement.CommissionAmount.ToString(CultureInfo.InvariantCulture)); update.Parameters.AddWithValue("$net",settlement.NetAmount.ToString(CultureInfo.InvariantCulture)); update.Parameters.AddWithValue("$status",status); update.Parameters.AddWithValue("$now",now); update.Parameters.AddWithValue("$id",settlement.ConsignorSettlementId); update.ExecuteNonQuery();
            }
            WriteAudit(connection,transaction,"ConsignorPayoutRecorded",$"Settlement #{settlement.ConsignorSettlementId}, method={payout.Method}, amount={payout.Amount.ToString(CultureInfo.InvariantCulture)}, status={status}");
            transaction.Commit(); return LoadConsignorSettlement(settlement.ConsignorNumber);
        }
    }

    public void SaveBidder(AuctionBidder bidder, int? originalBidderNumber)
    {
        ArgumentNullException.ThrowIfNull(bidder);

        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            var auctionId = GetCurrentAuctionId(connection);
            if (bidder.AuctionId != auctionId) throw new InvalidOperationException("Bidder registration must use the current auction.");

            long buyerId = bidder.BuyerId;
            using (var buyer = connection.CreateCommand())
            {
                buyer.Transaction = transaction;
                buyer.CommandText = buyerId == 0
                    ? """
                        INSERT INTO Buyers(FirstName,LastName,DriversLicense,Address,City,State,Zip,Phone,Email,CreatedUtc,UpdatedUtc)
                        VALUES($first,$last,$license,$address,$city,$state,$zip,$phone,$email,$created,$updated);
                        SELECT last_insert_rowid();
                        """
                    : """
                        UPDATE Buyers SET FirstName=$first,LastName=$last,DriversLicense=$license,Address=$address,City=$city,State=$state,
                            Zip=$zip,Phone=$phone,Email=$email,UpdatedUtc=$updated WHERE BuyerId=$buyerId;
                        SELECT $buyerId;
                        """;
                AddBuyerParameters(buyer, bidder);
                buyer.Parameters.AddWithValue("$buyerId", buyerId);
                buyerId = Convert.ToInt64(buyer.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO AuctionBidders(AuctionId,BuyerId,BidderNumber,Notes,RegisteredUtc,UpdatedUtc)
                VALUES($auctionId,$buyerId,$number,$notes,$created,$updated)
                ON CONFLICT(AuctionId,BidderNumber) DO UPDATE SET BuyerId=excluded.BuyerId,Notes=excluded.Notes,UpdatedUtc=excluded.UpdatedUtc;
                """;
            if (originalBidderNumber.HasValue && originalBidderNumber.Value != bidder.BidderNumber)
            {
                using var renumber = connection.CreateCommand();
                renumber.Transaction = transaction;
                renumber.CommandText = "UPDATE AuctionBidders SET BidderNumber=$number WHERE AuctionId=$auctionId AND BidderNumber=$original;";
                renumber.Parameters.AddWithValue("$number", bidder.BidderNumber); renumber.Parameters.AddWithValue("$auctionId", auctionId);
                renumber.Parameters.AddWithValue("$original", originalBidderNumber.Value);
                renumber.ExecuteNonQuery();
            }
            AddRegistrationParameters(command, bidder, buyerId);
            command.ExecuteNonQuery();
            using var invoice = connection.CreateCommand();
            invoice.Transaction = transaction;
            invoice.CommandText = """
                INSERT OR IGNORE INTO Invoices(AuctionId,AuctionBidderId,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,
                    HammerSubtotal,BuyerPremiumAmount,SalesTaxAmount,TotalAmount,Status,CreatedUtc,UpdatedUtc)
                SELECT ab.AuctionId,ab.AuctionBidderId,a.BuyerPremiumPercent,a.SalesTaxPercent,a.SalesTaxBasis,
                    '0','0','0','0','Unpaid',$created,$updated
                FROM AuctionBidders ab JOIN Auctions a ON a.AuctionId=ab.AuctionId
                WHERE ab.AuctionId=$auctionId AND ab.BidderNumber=$number;
                """;
            invoice.Parameters.AddWithValue("$auctionId", auctionId); invoice.Parameters.AddWithValue("$number", bidder.BidderNumber);
            invoice.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            invoice.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            invoice.ExecuteNonQuery();
            WriteAudit(connection, transaction, "BidderSaved", $"Bidder #{bidder.BidderNumber}: {bidder.FirstName} {bidder.LastName}".Trim());
            transaction.Commit();
        }
    }

    public void DeleteBidder(int bidderNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction=connection.BeginTransaction(); var auctionId=GetCurrentAuctionId(connection);
            using (var invoice=connection.CreateCommand())
            {
                invoice.Transaction=transaction; invoice.CommandText="""
                    DELETE FROM Invoices WHERE AuctionBidderId=(SELECT AuctionBidderId FROM AuctionBidders WHERE AuctionId=$auctionId AND BidderNumber=$number)
                    AND NOT EXISTS(SELECT 1 FROM Payments WHERE Payments.InvoiceId=Invoices.InvoiceId)
                    AND NOT EXISTS(SELECT 1 FROM Sales WHERE Sales.AuctionBidderId=Invoices.AuctionBidderId);
                    """;
                invoice.Parameters.AddWithValue("$auctionId",auctionId); invoice.Parameters.AddWithValue("$number",bidderNumber); invoice.ExecuteNonQuery();
            }
            using var command=connection.CreateCommand(); command.Transaction=transaction;
            command.CommandText="DELETE FROM AuctionBidders WHERE AuctionId=$auctionId AND BidderNumber=$number;";
            command.Parameters.AddWithValue("$auctionId",auctionId); command.Parameters.AddWithValue("$number",bidderNumber);
            try { if(command.ExecuteNonQuery()!=1) throw new InvalidOperationException($"Bidder #{bidderNumber} was not found."); }
            catch(SqliteException exception) when(exception.SqliteErrorCode==19) { throw new InvalidOperationException("A bidder with purchases or payments cannot be removed from the auction.",exception); }
            WriteAudit(connection,transaction,"BidderDeleted",$"Bidder #{bidderNumber}"); transaction.Commit();
        }
    }

    public void SaveLot(AuctionLot lot, int? originalLotNumber)
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
                deleteOld.CommandText = "DELETE FROM AuctionLots WHERE AuctionId = $auctionId AND LotNumber = $original;";
                deleteOld.Parameters.AddWithValue("$auctionId", lot.AuctionId);
                deleteOld.Parameters.AddWithValue("$original", originalLotNumber.Value);
                deleteOld.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO AuctionLots
                (LotNumber, Description, Category, StartingBid, Status, AuctionId, ConsignorId, ReservePrice, PassOutMode, LotStatus, CreatedUtc, UpdatedUtc)
                VALUES
                ($number, $description, $category, $startingBid, $status, $auctionId, $consignorId, $reservePrice, $passOutMode, $lotStatus, $created, $updated)
                ON CONFLICT(AuctionId, LotNumber) DO UPDATE SET
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
            command.CommandText = "DELETE FROM AuctionLots WHERE AuctionId = $auctionId AND LotNumber = $number;";
            command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
            command.Parameters.AddWithValue("$number", lotNumber);
            command.ExecuteNonQuery();
            WriteAudit(connection, null, "LotDeleted", $"Lot #{lotNumber}");
        }
    }

    public Auction GetCurrentAuction()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            var currentAuctionId = GetCurrentAuctionId(connection);
            return GetAuction(connection, currentAuctionId);
        }
    }

    public IReadOnlyList<Auction> GetAuctions()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = AuctionSelectSql + " ORDER BY AuctionDate DESC, AuctionTime DESC, AuctionId DESC;";
            using var reader = command.ExecuteReader();
            var auctions = new List<Auction>();
            while (reader.Read()) auctions.Add(ReadAuction(reader));
            return auctions;
        }
    }

    public Auction SaveAuction(Auction auction)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = auction.AuctionId == 0
                ? """
                    INSERT INTO Auctions(AuctionNumber, AuctionName, AuctionDate, AuctionTime, Location,CompanyName,CompanyAddress,CompanyCity,CompanyState,CompanyZip,CompanyPhone,CompanyEmail,Auctioneer, Clerk, Cashier,
                        BuyerPremiumPercent,CreditCardFeePercent,CreditCardFeeFixedAmount,DefaultConsignorCommissionPercent,SalesTaxPercent,SalesTaxBasis,InvoiceFooter,ReceiptFooter,
                        Status, CreatedDate, CreatedUtc, UpdatedUtc)
                    VALUES($number, $name, $date, $time, $location,$companyName,$companyAddress,$companyCity,$companyState,$companyZip,$companyPhone,$companyEmail,$auctioneer, $clerk, $cashier,
                        $premium,$cardFee,$cardFixedFee,$commission,$tax,$taxBasis,$invoiceFooter,$receiptFooter,$status,$created,$created,$updated);
                    SELECT last_insert_rowid();
                    """
                : """
                    UPDATE Auctions SET AuctionNumber=$number, AuctionName=$name, AuctionDate=$date, AuctionTime=$time,
                        Location=$location,CompanyName=$companyName,CompanyAddress=$companyAddress,CompanyCity=$companyCity,CompanyState=$companyState,CompanyZip=$companyZip,CompanyPhone=$companyPhone,CompanyEmail=$companyEmail,Auctioneer=$auctioneer, Clerk=$clerk, Cashier=$cashier,
                        BuyerPremiumPercent=$premium,CreditCardFeePercent=$cardFee,CreditCardFeeFixedAmount=$cardFixedFee,DefaultConsignorCommissionPercent=$commission,SalesTaxPercent=$tax,SalesTaxBasis=$taxBasis,
                        InvoiceFooter=$invoiceFooter, ReceiptFooter=$receiptFooter, Status=$status, UpdatedUtc=$updated
                    WHERE AuctionId=$id;
                    SELECT $id;
                    """;
            AddAuctionParameters(command, auction, now);
            auction.AuctionId = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            WriteAudit(connection, transaction, "AuctionSaved", $"Auction {auction.AuctionNumber}: {auction.AuctionName}");
            transaction.Commit();
            return GetAuction(connection, auction.AuctionId);
        }
    }

    public void SelectAuction(long auctionId)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            _ = GetAuction(connection, auctionId);
            using var transaction = connection.BeginTransaction();
            SaveSetting(connection, transaction, "CurrentAuctionId", auctionId.ToString(CultureInfo.InvariantCulture));
            WriteAudit(connection, transaction, "AuctionSelected", $"Auction #{auctionId}");
            transaction.Commit();
        }
    }

    public void CloseAuction(long auctionId)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE Auctions SET Status='Closed', UpdatedUtc=$updated WHERE AuctionId=$id AND lower(Status) <> 'closed';";
            command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$id", auctionId);
            if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException("The auction was not found or is already closed.");
            WriteAudit(connection, transaction, "AuctionClosed", $"Auction #{auctionId}");
            transaction.Commit();
        }
    }

    public void CopyConsignorAssociations(long sourceAuctionId, long destinationAuctionId)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR IGNORE INTO AuctionConsignors(AuctionId, ConsignorId, CreatedDate)
                SELECT $destination, ConsignorId, $created FROM AuctionConsignors WHERE AuctionId=$source;
                """;
            command.Parameters.AddWithValue("$destination", destinationAuctionId);
            command.Parameters.AddWithValue("$source", sourceAuctionId);
            command.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
            WriteAudit(connection, transaction, "AuctionConsignorsCopied", $"Auction #{sourceAuctionId} to #{destinationAuctionId}");
            transaction.Commit();
        }
    }

    public void CopyBidderRegistrations(long sourceAuctionId, long destinationAuctionId)
    {
        if (sourceAuctionId == destinationAuctionId) throw new InvalidOperationException("Choose a different auction to receive the copied bidders.");
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            using (var registrations = connection.CreateCommand())
            {
                registrations.Transaction = transaction;
                registrations.CommandText = """
                    INSERT OR IGNORE INTO AuctionBidders(AuctionId,BuyerId,BidderNumber,Notes,RegisteredUtc,UpdatedUtc)
                    SELECT $destination,BuyerId,BidderNumber,Notes,$now,$now
                    FROM AuctionBidders WHERE AuctionId=$source;
                    """;
                registrations.Parameters.AddWithValue("$destination", destinationAuctionId);
                registrations.Parameters.AddWithValue("$source", sourceAuctionId);
                registrations.Parameters.AddWithValue("$now", now);
                registrations.ExecuteNonQuery();
            }
            using (var invoices = connection.CreateCommand())
            {
                invoices.Transaction = transaction;
                invoices.CommandText = """
                    INSERT OR IGNORE INTO Invoices(AuctionId,AuctionBidderId,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,
                        HammerSubtotal,BuyerPremiumAmount,SalesTaxAmount,CreditCardFeeAmount,TotalAmount,Status,CreatedUtc,UpdatedUtc)
                    SELECT ab.AuctionId,ab.AuctionBidderId,a.BuyerPremiumPercent,a.SalesTaxPercent,a.SalesTaxBasis,
                        '0','0','0','0','0','Unpaid',$now,$now
                    FROM AuctionBidders ab JOIN Auctions a ON a.AuctionId=ab.AuctionId
                    WHERE ab.AuctionId=$destination;
                    """;
                invoices.Parameters.AddWithValue("$destination", destinationAuctionId);
                invoices.Parameters.AddWithValue("$now", now);
                invoices.ExecuteNonQuery();
            }
            WriteAudit(connection, transaction, "AuctionBiddersCopied", $"Auction #{sourceAuctionId} to #{destinationAuctionId}; registrations only");
            transaction.Commit();
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
                       PrintCheck, HoldCheck, TaxExempt, Require1099, CreatedDate, LinkedBuyerId
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
                    PrintCheck = reader.GetInt32(16) != 0, HoldCheck = reader.GetInt32(17) != 0, TaxExempt = reader.GetInt32(18) != 0,
                    Require1099 = reader.GetInt32(19) != 0, CreatedDate = DateTime.Parse(reader.GetString(20), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),LinkedBuyerId=reader.IsDBNull(21)?null:reader.GetInt64(21)
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
                if (renumber.ExecuteNonQuery() != 1) throw new InvalidOperationException("The seller to update was not found.");
            }

            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO Consignors
                (ConsignorNumber, BusinessName, FirstName, LastName, Address, City, State, Zip, Phone, Email, TaxId,
                 DefaultCommissionPercent, CommissionOverridePercent, Notes, Active, PrintCheck, HoldCheck, TaxExempt, Is1099Required, Require1099, CreatedDate, CreatedUtc, UpdatedUtc)
                VALUES
                ($number, $business, $first, $last, $address, $city, $state, $zip, $phone, $email, $taxId,
                 $defaultCommission, $overrideCommission, $notes, $active, $printCheck, $holdCheck, $taxExempt, $required1099, $required1099, $createdDate, $created, $updated)
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
                    PrintCheck = excluded.PrintCheck, HoldCheck = excluded.HoldCheck, TaxExempt = excluded.TaxExempt,
                    Is1099Required = excluded.Is1099Required, Require1099 = excluded.Require1099,
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
            if (command.ExecuteNonQuery() != 1) throw new InvalidOperationException($"Seller #{consignorNumber} was not found.");
            WriteAudit(connection, transaction, "ConsignorDeleted", $"Consignor #{consignorNumber}");
            transaction.Commit();
        }
    }

    public void AssociateConsignorWithCurrentAuction(int consignorNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR IGNORE INTO AuctionConsignors(AuctionId, ConsignorId, CreatedDate)
                SELECT $auctionId, ConsignorId, $created FROM Consignors WHERE ConsignorNumber=$number;
                """;
            command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
            command.Parameters.AddWithValue("$number", consignorNumber);
            command.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    public void ValidateSellerBuyerNumber(int sellerNumber, long? linkedBuyerId)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            var auctionId = GetCurrentAuctionId(connection);
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT BuyerId FROM AuctionBidders WHERE AuctionId=$auctionId AND BidderNumber=$number;";
            command.Parameters.AddWithValue("$auctionId", auctionId);
            command.Parameters.AddWithValue("$number", sellerNumber);
            var existing = command.ExecuteScalar();
            if (existing is not null)
            {
                var buyerId = Convert.ToInt64(existing, CultureInfo.InvariantCulture);
                if (linkedBuyerId == buyerId) return;
                throw new InvalidOperationException($"Buyer Number #{sellerNumber} is already registered to another buyer in this auction. Choose a different Seller Number.");
            }
            if (!linkedBuyerId.HasValue) return;

            using var linkedRegistration = connection.CreateCommand();
            linkedRegistration.CommandText = "SELECT BidderNumber FROM AuctionBidders WHERE AuctionId=$auctionId AND BuyerId=$buyerId;";
            linkedRegistration.Parameters.AddWithValue("$auctionId", auctionId);
            linkedRegistration.Parameters.AddWithValue("$buyerId", linkedBuyerId.Value);
            var registeredNumber = linkedRegistration.ExecuteScalar();
            if (registeredNumber is null) return;
            throw new InvalidOperationException($"This seller is already linked to Buyer Number #{Convert.ToInt32(registeredNumber, CultureInfo.InvariantCulture)} in the current auction. Use that number as the Seller Number, or remove that buyer registration before choosing a different number.");
        }
    }

    public void EnsureConsignorBuyerLink(int consignorNumber)
    {
        lock(_sync)
        {
            using var connection=OpenConnection();using var transaction=connection.BeginTransaction();var auctionId=GetCurrentAuctionId(connection);
            long consignorId;long? linked;string first,last,address,city,state,zip,phone,email;
            using(var c=connection.CreateCommand()){c.Transaction=transaction;c.CommandText="SELECT ConsignorId,LinkedBuyerId,FirstName,LastName,Address,City,State,Zip,Phone,Email FROM Consignors WHERE ConsignorNumber=$n;";c.Parameters.AddWithValue("$n",consignorNumber);using var r=c.ExecuteReader();if(!r.Read())throw new InvalidOperationException($"Seller #{consignorNumber} was not found.");consignorId=r.GetInt64(0);linked=r.IsDBNull(1)?null:r.GetInt64(1);first=r.GetString(2);last=r.GetString(3);address=r.GetString(4);city=r.GetString(5);state=r.GetString(6);zip=r.GetString(7);phone=r.GetString(8);email=r.GetString(9);}
            long buyerId;
            using(var find=connection.CreateCommand()){find.Transaction=transaction;find.CommandText="SELECT BuyerId FROM AuctionBidders WHERE AuctionId=$a AND BidderNumber=$n;";find.Parameters.AddWithValue("$a",auctionId);find.Parameters.AddWithValue("$n",consignorNumber);var value=find.ExecuteScalar();buyerId=value is null?0:Convert.ToInt64(value,CultureInfo.InvariantCulture);}
            if(linked.HasValue&&buyerId>0&&linked.Value!=buyerId)throw new InvalidOperationException($"Buyer Number #{consignorNumber} belongs to a different buyer. Choose a different Seller Number.");
            var now=DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture);
            if(buyerId==0)
            {
                if(linked.HasValue)buyerId=linked.Value;else{using var b=connection.CreateCommand();b.Transaction=transaction;b.CommandText="INSERT INTO Buyers(FirstName,LastName,DriversLicense,Address,City,State,Zip,Phone,Email,CreatedUtc,UpdatedUtc) VALUES($f,$l,'',$ad,$c,$s,$z,$p,$e,$now,$now);SELECT last_insert_rowid();";b.Parameters.AddWithValue("$f",first);b.Parameters.AddWithValue("$l",last);b.Parameters.AddWithValue("$ad",address);b.Parameters.AddWithValue("$c",city);b.Parameters.AddWithValue("$s",state);b.Parameters.AddWithValue("$z",zip);b.Parameters.AddWithValue("$p",phone);b.Parameters.AddWithValue("$e",email);b.Parameters.AddWithValue("$now",now);buyerId=Convert.ToInt64(b.ExecuteScalar(),CultureInfo.InvariantCulture);}
                using var ab=connection.CreateCommand();ab.Transaction=transaction;ab.CommandText="INSERT INTO AuctionBidders(AuctionId,BuyerId,BidderNumber,Notes,RegisteredUtc,UpdatedUtc) VALUES($a,$b,$n,'Linked consignor',$now,$now);SELECT last_insert_rowid();";ab.Parameters.AddWithValue("$a",auctionId);ab.Parameters.AddWithValue("$b",buyerId);ab.Parameters.AddWithValue("$n",consignorNumber);ab.Parameters.AddWithValue("$now",now);var registration=Convert.ToInt64(ab.ExecuteScalar(),CultureInfo.InvariantCulture);using var inv=connection.CreateCommand();inv.Transaction=transaction;inv.CommandText="INSERT INTO Invoices(AuctionId,AuctionBidderId,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,HammerSubtotal,BuyerPremiumAmount,SalesTaxAmount,TotalAmount,Status,CreatedUtc,UpdatedUtc) SELECT AuctionId,$r,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,'0','0','0','0','Unpaid',$now,$now FROM Auctions WHERE AuctionId=$a;";inv.Parameters.AddWithValue("$r",registration);inv.Parameters.AddWithValue("$a",auctionId);inv.Parameters.AddWithValue("$now",now);inv.ExecuteNonQuery();
            }
            using(var update=connection.CreateCommand()){update.Transaction=transaction;update.CommandText="UPDATE Consignors SET LinkedBuyerId=$b WHERE ConsignorId=$c;";update.Parameters.AddWithValue("$b",buyerId);update.Parameters.AddWithValue("$c",consignorId);update.ExecuteNonQuery();}
            WriteAudit(connection,transaction,"ConsignorBuyerLinked",$"Consignor #{consignorNumber}, BuyerId={buyerId}, AuctionId={auctionId}");transaction.Commit();
        }
    }

    public AuctionSettlementReport LoadAuctionSettlementReport()
    {
        lock(_sync)
        {
            using(var seed=OpenConnection()){var a=GetCurrentAuctionId(seed);using var q=seed.CreateCommand();q.CommandText="SELECT c.ConsignorNumber FROM AuctionConsignors ac JOIN Consignors c ON c.ConsignorId=ac.ConsignorId WHERE ac.AuctionId=$a;";q.Parameters.AddWithValue("$a",a);using var r=q.ExecuteReader();var numbers=new List<int>();while(r.Read())numbers.Add(r.GetInt32(0));r.Close();foreach(var n in numbers)LoadConsignorSettlement(n);}
            using var connection=OpenConnection();var auction=GetAuction(connection,GetCurrentAuctionId(connection));var report=new AuctionSettlementReport{AuctionId=auction.AuctionId,AuctionNumber=auction.AuctionNumber,AuctionName=auction.AuctionName};
            decimal Sum(string sql){using var c=connection.CreateCommand();c.CommandText=sql;c.Parameters.AddWithValue("$a",auction.AuctionId);return Convert.ToDecimal(c.ExecuteScalar()??0,CultureInfo.InvariantCulture);}
            report.HammerSales=Sum("SELECT COALESCE(SUM(CAST(HammerPrice AS REAL)*Quantity),0) FROM Sales WHERE AuctionId=$a AND IsVoided=0;");report.BuyerPremiums=Sum("SELECT COALESCE(SUM(CAST(BuyerPremiumAmount AS REAL)),0) FROM Invoices WHERE AuctionId=$a;");report.CreditCardFees=Sum("SELECT COALESCE(SUM(CAST(CreditCardFeeAmount AS REAL)),0) FROM Invoices WHERE AuctionId=$a;");report.SalesTaxCollected=Sum("SELECT COALESCE(SUM(CAST(SalesTaxAmount AS REAL)),0) FROM Invoices WHERE AuctionId=$a;");report.InvoiceTotal=Sum("SELECT COALESCE(SUM(CAST(TotalAmount AS REAL)),0) FROM Invoices WHERE AuctionId=$a;");report.InvoiceAmountPaid=Sum("SELECT COALESCE(SUM(CAST(p.AppliedAmount AS REAL)),0) FROM Payments p JOIN Invoices i ON i.InvoiceId=p.InvoiceId WHERE i.AuctionId=$a;")-Sum("SELECT COALESCE(SUM(CAST(r.Amount AS REAL)),0) FROM Refunds r JOIN Invoices i ON i.InvoiceId=r.InvoiceId WHERE i.AuctionId=$a;");report.InvoiceBalanceUnpaid=Math.Max(0,report.InvoiceTotal-report.InvoiceAmountPaid);report.ConsignorGrossSales=Sum("SELECT COALESCE(SUM(CAST(HammerSubtotal AS REAL)),0) FROM ConsignorSettlements WHERE AuctionId=$a;");report.ConsignorCommissions=Sum("SELECT COALESCE(SUM(CAST(CommissionAmount AS REAL)),0) FROM ConsignorSettlements WHERE AuctionId=$a;");report.PurchaseBalanceOffsets=Sum("SELECT COALESCE(SUM(CAST(HammerSubtotal AS REAL)-CAST(CommissionAmount AS REAL)-CAST(NetAmount AS REAL)),0) FROM ConsignorSettlements WHERE AuctionId=$a;");report.ConsignorPayouts=Sum("SELECT COALESCE(SUM(CAST(cp.Amount AS REAL)),0) FROM ConsignorPayouts cp JOIN ConsignorSettlements cs ON cs.ConsignorSettlementId=cp.ConsignorSettlementId WHERE cs.AuctionId=$a;");
            using var ledger=connection.CreateCommand();ledger.CommandText="SELECT StaffPaymentId,AuctionId,Category,Payee,Amount,Method,PaidUtc,Notes,VoidsStaffPaymentId,ReplacementForStaffPaymentId,VoidReason FROM StaffPayments WHERE AuctionId=$a ORDER BY StaffPaymentId;";ledger.Parameters.AddWithValue("$a",auction.AuctionId);using var reader=ledger.ExecuteReader();while(reader.Read())report.StaffLedger.Add(ReadStaff(reader));report.StaffPayments=report.StaffLedger.Sum(x=>x.Amount);return report;
        }
    }
    public StaffPayment RecordStaffPayment(StaffPayment payment){lock(_sync){using var c=OpenConnection();using var t=c.BeginTransaction();payment.AuctionId=GetCurrentAuctionId(c);payment.PaidUtc=payment.PaidUtc==default?DateTime.UtcNow:payment.PaidUtc;payment.StaffPaymentId=InsertStaff(c,t,payment);WriteAudit(c,t,"StaffPaymentRecorded",$"Staff payment #{payment.StaffPaymentId}, {payment.Payee}, {payment.Amount:C2}");t.Commit();return payment;}}
    public StaffPayment VoidStaffPayment(long id,string reason,StaffPayment? replacement){lock(_sync){using var c=OpenConnection();using var t=c.BeginTransaction();StaffPayment original;using(var q=c.CreateCommand()){q.Transaction=t;q.CommandText="SELECT StaffPaymentId,AuctionId,Category,Payee,Amount,Method,PaidUtc,Notes,VoidsStaffPaymentId,ReplacementForStaffPaymentId,VoidReason FROM StaffPayments WHERE StaffPaymentId=$id AND AuctionId=$a;";q.Parameters.AddWithValue("$id",id);q.Parameters.AddWithValue("$a",GetCurrentAuctionId(c));using var r=q.ExecuteReader();if(!r.Read())throw new InvalidOperationException("Staff payment was not found in the current auction.");original=ReadStaff(r);}using(var dupe=c.CreateCommand()){dupe.Transaction=t;dupe.CommandText="SELECT COUNT(*) FROM StaffPayments WHERE VoidsStaffPaymentId=$id;";dupe.Parameters.AddWithValue("$id",id);if(Convert.ToInt32(dupe.ExecuteScalar(),CultureInfo.InvariantCulture)>0)throw new InvalidOperationException("That staff payment is already voided.");}var entry=new StaffPayment{AuctionId=original.AuctionId,Category=original.Category,Payee=original.Payee,Amount=-original.Amount,Method=original.Method,PaidUtc=DateTime.UtcNow,Notes=original.Notes,VoidsStaffPaymentId=id,VoidReason=reason.Trim()};entry.StaffPaymentId=InsertStaff(c,t,entry);if(replacement is not null){replacement.AuctionId=original.AuctionId;replacement.PaidUtc=DateTime.UtcNow;replacement.StaffPaymentId=InsertStaff(c,t,replacement);}WriteAudit(c,t,"StaffPaymentVoided",$"Staff payment #{id}, reason={reason}, replacement={replacement?.StaffPaymentId}");t.Commit();return entry;}}
    private static long InsertStaff(SqliteConnection c,SqliteTransaction t,StaffPayment p){using var q=c.CreateCommand();q.Transaction=t;q.CommandText="INSERT INTO StaffPayments(AuctionId,Category,Payee,Amount,Method,PaidUtc,Notes,VoidsStaffPaymentId,ReplacementForStaffPaymentId,VoidReason) VALUES($a,$c,$p,$m,$method,$utc,$notes,$voids,$replacement,$reason);SELECT last_insert_rowid();";q.Parameters.AddWithValue("$a",p.AuctionId);q.Parameters.AddWithValue("$c",p.Category);q.Parameters.AddWithValue("$p",p.Payee);q.Parameters.AddWithValue("$m",p.Amount.ToString(CultureInfo.InvariantCulture));q.Parameters.AddWithValue("$method",p.Method);q.Parameters.AddWithValue("$utc",p.PaidUtc.ToString("O",CultureInfo.InvariantCulture));q.Parameters.AddWithValue("$notes",p.Notes??"");q.Parameters.AddWithValue("$voids",(object?)p.VoidsStaffPaymentId??DBNull.Value);q.Parameters.AddWithValue("$replacement",(object?)p.ReplacementForStaffPaymentId??DBNull.Value);q.Parameters.AddWithValue("$reason",p.VoidReason??"");return Convert.ToInt64(q.ExecuteScalar(),CultureInfo.InvariantCulture);}
    private static StaffPayment ReadStaff(SqliteDataReader r)=>new(){StaffPaymentId=r.GetInt64(0),AuctionId=r.GetInt64(1),Category=r.GetString(2),Payee=r.GetString(3),Amount=decimal.Parse(r.GetString(4),CultureInfo.InvariantCulture),Method=r.GetString(5),PaidUtc=DateTime.Parse(r.GetString(6),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind),Notes=r.GetString(7),VoidsStaffPaymentId=r.IsDBNull(8)?null:r.GetInt64(8),ReplacementForStaffPaymentId=r.IsDBNull(9)?null:r.GetInt64(9),VoidReason=r.GetString(10)};



    public Sale RecordSale(int lotNumber, int bidderNumber, decimal hammerPrice, bool keepLotOpen = false, int quantity = 1)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            var auctionId = GetCurrentAuctionId(connection);
            long auctionLotId;
            using (var verify = connection.CreateCommand())
            {
                verify.Transaction = transaction;
                verify.CommandText = "SELECT AuctionLotId, Status FROM AuctionLots WHERE AuctionId=$auctionId AND LotNumber=$lot LIMIT 1;";
                verify.Parameters.AddWithValue("$auctionId", auctionId);
                verify.Parameters.AddWithValue("$lot", lotNumber);
                using var reader = verify.ExecuteReader();
                if (!reader.Read()) throw new InvalidOperationException($"Lot #{lotNumber} was not found.");
                auctionLotId = reader.GetInt64(0);
                var status = reader.GetString(1);
                if (string.Equals(status, "Sold", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Lot #{lotNumber} is closed and has already been sold.");
            }

            long auctionBidderId;
            using (var bidderCheck = connection.CreateCommand())
            {
                bidderCheck.Transaction = transaction;
                bidderCheck.CommandText = "SELECT AuctionBidderId FROM AuctionBidders WHERE AuctionId=$auctionId AND BidderNumber=$bidder;";
                bidderCheck.Parameters.AddWithValue("$auctionId", auctionId);
                bidderCheck.Parameters.AddWithValue("$bidder", bidderNumber);
                var value = bidderCheck.ExecuteScalar();
                if (value is null) throw new InvalidOperationException($"Bidder #{bidderNumber} was not found in the current auction.");
                auctionBidderId = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }

            var saleUtc = DateTime.UtcNow;
            long saleId;
            using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO Sales(AuctionId,AuctionLotId,AuctionBidderId,LotNumber,BidderNumber,HammerPrice,Quantity,SaleUtc,Paid)
                    VALUES($auctionId,$auctionLotId,$auctionBidderId,$lot,$bidder,$price,$quantity,$saleUtc,0);
                    SELECT last_insert_rowid();
                    """;
                insert.Parameters.AddWithValue("$lot", lotNumber);
                insert.Parameters.AddWithValue("$auctionId", auctionId);
                insert.Parameters.AddWithValue("$auctionLotId", auctionLotId);
                insert.Parameters.AddWithValue("$auctionBidderId", auctionBidderId);
                insert.Parameters.AddWithValue("$bidder", bidderNumber);
                insert.Parameters.AddWithValue("$price", hammerPrice.ToString(CultureInfo.InvariantCulture));
                insert.Parameters.AddWithValue("$quantity", quantity);
                insert.Parameters.AddWithValue("$saleUtc", saleUtc.ToString("O", CultureInfo.InvariantCulture));
                saleId = Convert.ToInt64(insert.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using (var updateLot = connection.CreateCommand())
            {
                updateLot.Transaction = transaction;
                updateLot.CommandText = "UPDATE AuctionLots SET Status=$status, LotStatus=$status, PassOutMode=$passOut, UpdatedUtc=$updated WHERE AuctionLotId=$auctionLotId;";
                updateLot.Parameters.AddWithValue("$status", keepLotOpen ? "Pass-Out" : "Sold");
                updateLot.Parameters.AddWithValue("$passOut", keepLotOpen ? 1 : 0);
                updateLot.Parameters.AddWithValue("$updated", saleUtc.ToString("O", CultureInfo.InvariantCulture));
                updateLot.Parameters.AddWithValue("$auctionLotId", auctionLotId);
                if (updateLot.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("The lot could not be marked sold.");
            }

            RefreshInvoiceTotals(connection,transaction,auctionBidderId);
            WriteAudit(connection, transaction, "SaleRecorded", $"Lot #{lotNumber}, bidder #{bidderNumber}, quantity={quantity}, unit={hammerPrice:C2}, total={quantity*hammerPrice:C2}, pass-out={keepLotOpen}");
            transaction.Commit();
            return new Sale
            {
                Id = saleId,
                AuctionId = auctionId,
                LotNumber = lotNumber,
                BidderNumber = bidderNumber,
                Quantity = quantity,
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
            command.CommandText = "UPDATE AuctionLots SET Status='Sold', LotStatus='Sold', PassOutMode=0, UpdatedUtc=$updated WHERE AuctionId=$auctionId AND LotNumber=$lot;";
            command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
            command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$lot", lotNumber);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Lot #{lotNumber} was not found.");
            WriteAudit(connection, null, "LotClosed", $"Lot #{lotNumber}");
        }
    }

    public void PassLot(int lotNumber)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();
            var auctionId = GetCurrentAuctionId(connection);
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "UPDATE AuctionLots SET Status='Passed', LotStatus='Passed', PassOutMode=0, UpdatedUtc=$updated WHERE AuctionId=$auctionId AND LotNumber=$lot AND Status NOT IN ('Sold','Passed');";
            command.Parameters.AddWithValue("$auctionId", auctionId);
            command.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$lot", lotNumber);
            if (command.ExecuteNonQuery() != 1)
                throw new InvalidOperationException($"Lot #{lotNumber} was not found or is already closed.");
            WriteAudit(connection, transaction, "LotPassed", $"Lot #{lotNumber}");
            transaction.Commit();
        }
    }

    public Sale? GetLastSale()
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT Id, AuctionId, LotNumber, BidderNumber, HammerPrice, SaleUtc, Paid, Quantity
                FROM Sales WHERE AuctionId=$auctionId AND IsVoided=0
                ORDER BY Id DESC
                LIMIT 1;
                """;
            command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return ReadSale(reader);
        }
    }

    public IReadOnlyList<Sale> GetSalesForLot(int lotNumber)
    {
        lock(_sync)
        {
            using var connection=OpenConnection();using var command=connection.CreateCommand();
            command.CommandText="SELECT Id,AuctionId,LotNumber,BidderNumber,HammerPrice,SaleUtc,Paid,Quantity FROM Sales WHERE AuctionId=$auctionId AND LotNumber=$lot AND IsVoided=0 ORDER BY Id;";
            command.Parameters.AddWithValue("$auctionId",GetCurrentAuctionId(connection));command.Parameters.AddWithValue("$lot",lotNumber);
            using var reader=command.ExecuteReader();var sales=new List<Sale>();while(reader.Read())sales.Add(ReadSale(reader));return sales;
        }
    }

    public void UndoLastSale(long saleId)
    {
        lock (_sync)
        {
            using var connection = OpenConnection();
            using var transaction = connection.BeginTransaction();

            int lotNumber;
            long auctionId;
            long auctionLotId;
            long auctionBidderId;
            using (var getSale = connection.CreateCommand())
            {
                getSale.Transaction = transaction;
                getSale.CommandText = "SELECT AuctionId,AuctionLotId,AuctionBidderId,LotNumber FROM Sales WHERE Id=$id LIMIT 1;";
                getSale.Parameters.AddWithValue("$id", saleId);
                using var reader = getSale.ExecuteReader();
                if (!reader.Read()) throw new InvalidOperationException("The sale could not be found.");
                auctionId=reader.GetInt64(0); auctionLotId=reader.GetInt64(1); auctionBidderId=reader.GetInt64(2); lotNumber=reader.GetInt32(3);
            }

            using (var paid=connection.CreateCommand())
            {
                paid.Transaction=transaction;
                paid.CommandText="SELECT COUNT(*) FROM Payments p JOIN Invoices i ON i.InvoiceId=p.InvoiceId WHERE i.AuctionBidderId=$registration;";
                paid.Parameters.AddWithValue("$registration",auctionBidderId);
                if(Convert.ToInt32(paid.ExecuteScalar(),CultureInfo.InvariantCulture)>0)
                    throw new InvalidOperationException("A sale on an invoice with payments cannot be undone.");
            }

            using (var ensureLast = connection.CreateCommand())
            {
                ensureLast.Transaction = transaction;
                ensureLast.CommandText = "SELECT Id FROM Sales WHERE AuctionId=$auctionId AND IsVoided=0 ORDER BY Id DESC LIMIT 1;";
                ensureLast.Parameters.AddWithValue("$auctionId", auctionId);
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
                countSales.CommandText = "SELECT COUNT(*) FROM Sales WHERE AuctionLotId=$auctionLotId AND IsVoided=0;";
                countSales.Parameters.AddWithValue("$auctionLotId", auctionLotId);
                remainingSales = Convert.ToInt32(countSales.ExecuteScalar(), CultureInfo.InvariantCulture);
            }

            using (var updateLot = connection.CreateCommand())
            {
                updateLot.Transaction = transaction;
                updateLot.CommandText = "UPDATE AuctionLots SET Status=$status, LotStatus=$status, PassOutMode=$passOut, UpdatedUtc=$updated WHERE AuctionLotId=$auctionLotId;";
                updateLot.Parameters.AddWithValue("$status", remainingSales > 0 ? "Pass-Out" : "Available");
                updateLot.Parameters.AddWithValue("$passOut", remainingSales > 0 ? 1 : 0);
                updateLot.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                updateLot.Parameters.AddWithValue("$auctionLotId", auctionLotId);
                updateLot.ExecuteNonQuery();
            }

            RefreshInvoiceTotals(connection,transaction,auctionBidderId);
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
            command.CommandText = "SELECT COUNT(*) FROM Sales WHERE AuctionId=$auctionId AND LotNumber=$lot AND IsVoided=0;";
            command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
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
                        (SELECT COUNT(*) FROM AuctionBidders WHERE AuctionId=$auctionId),
                        (SELECT COUNT(*) FROM AuctionLots WHERE AuctionId=$auctionId),
                        (SELECT COUNT(*) FROM AuctionLots WHERE AuctionId=$auctionId AND lower(Status)='sold'),
                        COALESCE((SELECT SUM(CAST(HammerPrice AS REAL)*Quantity) FROM Sales WHERE AuctionId=$auctionId AND IsVoided=0), 0),
                        COALESCE((SELECT AVG(CAST(HammerPrice AS REAL)*Quantity) FROM Sales WHERE AuctionId=$auctionId AND IsVoided=0), 0);
                    """;
                command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
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
                        SELECT 'Bidder #' || ab.BidderNumber || ' registered: ' || trim(b.FirstName || ' ' || b.LastName) AS ActivityText,
                               ab.UpdatedUtc AS ActivityUtc
                        FROM AuctionBidders ab JOIN Buyers b ON b.BuyerId=ab.BuyerId WHERE ab.AuctionId=$auctionId
                        UNION ALL
                        SELECT 'Lot #' || LotNumber || ' saved: ' || Description AS ActivityText,
                               UpdatedUtc AS ActivityUtc
                        FROM AuctionLots WHERE AuctionId=$auctionId
                        UNION ALL
                        SELECT 'Lot #' || LotNumber || ' sold to bidder #' || BidderNumber || ': ' || Quantity || ' x $' || HammerPrice AS ActivityText,
                               SaleUtc AS ActivityUtc
                        FROM Sales WHERE AuctionId=$auctionId AND IsVoided=0
                    )
                    ORDER BY ActivityUtc DESC
                    LIMIT 8;
                    """;
                command.Parameters.AddWithValue("$auctionId", GetCurrentAuctionId(connection));
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
        if (version < 5)
        {
            using var transaction = connection.BeginTransaction();
            using var migration = connection.CreateCommand();
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
            version = 5;
        }
        if (version < 6)
        {
            using var transaction = connection.BeginTransaction();
            using var migration = connection.CreateCommand();
            migration.Transaction = transaction;
            migration.CommandText = """
                ALTER TABLE Consignors ADD COLUMN Require1099 INTEGER NOT NULL DEFAULT 0;
                ALTER TABLE Consignors ADD COLUMN CreatedDate TEXT NOT NULL DEFAULT '';
                UPDATE Consignors SET Require1099 = Is1099Required,
                    CreatedDate = CASE WHEN CreatedUtc = '' THEN strftime('%Y-%m-%dT%H:%M:%fZ','now') ELSE CreatedUtc END;
                ALTER TABLE Auctions ADD COLUMN CreatedDate TEXT NOT NULL DEFAULT '';
                UPDATE Auctions SET CreatedDate = CASE WHEN CreatedUtc = '' THEN strftime('%Y-%m-%dT%H:%M:%fZ','now') ELSE CreatedUtc END;
                CREATE INDEX IF NOT EXISTS IX_Consignors_ConsignorNumber ON Consignors(ConsignorNumber);
                CREATE INDEX IF NOT EXISTS IX_Lots_LotNumber ON Lots(LotNumber);
                CREATE INDEX IF NOT EXISTS IX_Bidders_BidderNumber ON Bidders(BidderNumber);
                INSERT INTO Settings(Key, Value) VALUES ('DatabaseSchemaVersion', '6')
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
                """;
            migration.ExecuteNonQuery();
            transaction.Commit();
            version = 6;
        }
        if (version < 7)
        {
            using var transaction = connection.BeginTransaction();
            using var migration = connection.CreateCommand();
            migration.Transaction = transaction;
            migration.CommandText = """
                ALTER TABLE Auctions ADD COLUMN AuctionNumber TEXT NOT NULL DEFAULT '';
                ALTER TABLE Auctions ADD COLUMN AuctionTime TEXT NOT NULL DEFAULT '00:00:00';
                ALTER TABLE Auctions ADD COLUMN Auctioneer TEXT NOT NULL DEFAULT '';
                ALTER TABLE Auctions ADD COLUMN Clerk TEXT NOT NULL DEFAULT '';
                ALTER TABLE Auctions ADD COLUMN Cashier TEXT NOT NULL DEFAULT '';
                ALTER TABLE Auctions ADD COLUMN InvoiceFooter TEXT NOT NULL DEFAULT '';
                ALTER TABLE Auctions ADD COLUMN ReceiptFooter TEXT NOT NULL DEFAULT '';
                UPDATE Auctions SET AuctionNumber = 'LEGACY-' || AuctionId WHERE AuctionNumber = '';
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Auctions_AuctionNumber ON Auctions(AuctionNumber);

                CREATE TABLE AuctionConsignors (
                    AuctionId INTEGER NOT NULL,
                    ConsignorId INTEGER NOT NULL,
                    CreatedDate TEXT NOT NULL,
                    PRIMARY KEY (AuctionId, ConsignorId),
                    FOREIGN KEY (AuctionId) REFERENCES Auctions(AuctionId) ON DELETE CASCADE,
                    FOREIGN KEY (ConsignorId) REFERENCES Consignors(ConsignorId) ON DELETE CASCADE
                );
                CREATE INDEX IX_AuctionConsignors_ConsignorId ON AuctionConsignors(ConsignorId);
                INSERT OR IGNORE INTO AuctionConsignors(AuctionId, ConsignorId, CreatedDate)
                SELECT CAST((SELECT Value FROM Settings WHERE Key='CurrentAuctionId') AS INTEGER), ConsignorId,
                       strftime('%Y-%m-%dT%H:%M:%fZ','now') FROM Consignors;

                CREATE TABLE AuctionLots (
                    AuctionLotId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuctionId INTEGER NOT NULL,
                    ConsignorId INTEGER NOT NULL,
                    LotNumber INTEGER NOT NULL,
                    Description TEXT NOT NULL DEFAULT '', Category TEXT NOT NULL DEFAULT '', StartingBid TEXT NOT NULL DEFAULT '0',
                    Status TEXT NOT NULL DEFAULT 'Available', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL,
                    ReservePrice TEXT NOT NULL DEFAULT '0', PassOutMode INTEGER NOT NULL DEFAULT 0,
                    LotStatus TEXT NOT NULL DEFAULT 'Available',
                    UNIQUE (AuctionId, LotNumber),
                    FOREIGN KEY (AuctionId) REFERENCES Auctions(AuctionId) ON DELETE RESTRICT,
                    FOREIGN KEY (ConsignorId) REFERENCES Consignors(ConsignorId) ON DELETE RESTRICT
                );
                INSERT INTO AuctionLots(AuctionId, ConsignorId, LotNumber, Description, Category, StartingBid, Status,
                                        CreatedUtc, UpdatedUtc, ReservePrice, PassOutMode, LotStatus)
                SELECT AuctionId, ConsignorId, LotNumber, Description, Category, StartingBid, Status,
                       CreatedUtc, UpdatedUtc, ReservePrice, PassOutMode, LotStatus FROM Lots;

                CREATE TABLE Sales_New (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuctionId INTEGER NOT NULL, AuctionLotId INTEGER NOT NULL, LotNumber INTEGER NOT NULL, BidderNumber INTEGER NOT NULL,
                    HammerPrice TEXT NOT NULL DEFAULT '0', SaleUtc TEXT NOT NULL, Paid INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (AuctionLotId) REFERENCES AuctionLots(AuctionLotId) ON UPDATE CASCADE ON DELETE RESTRICT,
                    FOREIGN KEY (BidderNumber) REFERENCES Bidders(BidderNumber) ON UPDATE CASCADE ON DELETE RESTRICT
                );
                INSERT INTO Sales_New(Id, AuctionId, AuctionLotId, LotNumber, BidderNumber, HammerPrice, SaleUtc, Paid)
                SELECT s.Id, al.AuctionId, al.AuctionLotId, s.LotNumber, s.BidderNumber, s.HammerPrice, s.SaleUtc, s.Paid
                FROM Sales s JOIN AuctionLots al ON al.LotNumber = s.LotNumber;

                DROP TABLE Sales;
                ALTER TABLE Sales_New RENAME TO Sales;
                CREATE INDEX IX_AuctionLots_LotNumber ON AuctionLots(LotNumber);
                CREATE INDEX IX_AuctionLots_AuctionId ON AuctionLots(AuctionId);
                CREATE INDEX IX_AuctionLots_ConsignorId ON AuctionLots(ConsignorId);
                CREATE INDEX IX_AuctionLots_AuctionLotNumber ON AuctionLots(AuctionId, LotNumber);
                CREATE INDEX IX_AuctionLots_Description ON AuctionLots(Description);
                CREATE INDEX IX_AuctionLots_Category ON AuctionLots(Category);
                CREATE INDEX IX_AuctionLots_Status ON AuctionLots(Status);
                CREATE INDEX IX_Sales_AuctionId ON Sales(AuctionId);
                CREATE INDEX IX_Sales_AuctionLotId ON Sales(AuctionLotId);
                CREATE INDEX IX_Sales_BidderNumber ON Sales(BidderNumber);
                INSERT INTO Settings(Key, Value) VALUES ('DatabaseSchemaVersion', '7')
                ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
                """;
            migration.ExecuteNonQuery();
            transaction.Commit();
            version = 7;
        }
        if (version < 8)
        {
            using var transaction = connection.BeginTransaction();
            using var migration = connection.CreateCommand();
            migration.Transaction = transaction;
            migration.CommandText = """
                CREATE TABLE Buyers (
                    BuyerId INTEGER PRIMARY KEY AUTOINCREMENT,
                    FirstName TEXT NOT NULL DEFAULT '', LastName TEXT NOT NULL DEFAULT '', DriversLicense TEXT NOT NULL DEFAULT '',
                    Address TEXT NOT NULL DEFAULT '', City TEXT NOT NULL DEFAULT '', State TEXT NOT NULL DEFAULT '', Zip TEXT NOT NULL DEFAULT '',
                    Phone TEXT NOT NULL DEFAULT '', Email TEXT NOT NULL DEFAULT '', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL
                );
                CREATE INDEX IX_Buyers_Name ON Buyers(LastName,FirstName);
                CREATE INDEX IX_Buyers_DriversLicense ON Buyers(DriversLicense);
                CREATE INDEX IX_Buyers_Phone ON Buyers(Phone);
                CREATE INDEX IX_Buyers_Email ON Buyers(Email);
                INSERT INTO Buyers(BuyerId,FirstName,LastName,DriversLicense,Address,City,State,Zip,Phone,Email,CreatedUtc,UpdatedUtc)
                SELECT Id,FirstName,LastName,DriversLicense,Address,City,State,Zip,Phone,Email,CreatedUtc,UpdatedUtc FROM Bidders;

                CREATE TABLE AuctionBidders (
                    AuctionBidderId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuctionId INTEGER NOT NULL, BuyerId INTEGER NOT NULL, BidderNumber INTEGER NOT NULL,
                    Notes TEXT NOT NULL DEFAULT '', RegisteredUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL,
                    UNIQUE(AuctionId,BidderNumber), UNIQUE(AuctionId,BuyerId),
                    FOREIGN KEY(AuctionId) REFERENCES Auctions(AuctionId) ON DELETE CASCADE,
                    FOREIGN KEY(BuyerId) REFERENCES Buyers(BuyerId) ON DELETE RESTRICT
                );
                INSERT INTO AuctionBidders(AuctionId,BuyerId,BidderNumber,Notes,RegisteredUtc,UpdatedUtc)
                SELECT CAST((SELECT Value FROM Settings WHERE Key='CurrentAuctionId') AS INTEGER), Id, BidderNumber, Notes, CreatedUtc, UpdatedUtc FROM Bidders;
                INSERT OR IGNORE INTO AuctionBidders(AuctionId,BuyerId,BidderNumber,Notes,RegisteredUtc,UpdatedUtc)
                SELECT DISTINCT s.AuctionId,b.Id,b.BidderNumber,b.Notes,b.CreatedUtc,b.UpdatedUtc
                FROM Sales s JOIN Bidders b ON b.BidderNumber=s.BidderNumber;
                CREATE INDEX IX_AuctionBidders_BuyerId ON AuctionBidders(BuyerId);
                CREATE INDEX IX_AuctionBidders_BidderNumber ON AuctionBidders(BidderNumber);
                CREATE INDEX IX_AuctionBidders_AuctionBidderNumber ON AuctionBidders(AuctionId,BidderNumber);

                CREATE TABLE Sales_v8 (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT, AuctionId INTEGER NOT NULL, AuctionLotId INTEGER NOT NULL,
                    AuctionBidderId INTEGER NOT NULL, LotNumber INTEGER NOT NULL, BidderNumber INTEGER NOT NULL,
                    HammerPrice TEXT NOT NULL DEFAULT '0', SaleUtc TEXT NOT NULL, Paid INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY(AuctionId) REFERENCES Auctions(AuctionId) ON DELETE RESTRICT,
                    FOREIGN KEY(AuctionLotId) REFERENCES AuctionLots(AuctionLotId) ON DELETE RESTRICT,
                    FOREIGN KEY(AuctionBidderId) REFERENCES AuctionBidders(AuctionBidderId) ON DELETE RESTRICT
                );
                INSERT INTO Sales_v8(Id,AuctionId,AuctionLotId,AuctionBidderId,LotNumber,BidderNumber,HammerPrice,SaleUtc,Paid)
                SELECT s.Id,s.AuctionId,s.AuctionLotId,ab.AuctionBidderId,s.LotNumber,s.BidderNumber,s.HammerPrice,s.SaleUtc,s.Paid
                FROM Sales s JOIN AuctionBidders ab ON ab.AuctionId=s.AuctionId AND ab.BidderNumber=s.BidderNumber;
                DROP TABLE Sales;
                ALTER TABLE Sales_v8 RENAME TO Sales;
                CREATE INDEX IX_Sales_AuctionId ON Sales(AuctionId);
                CREATE INDEX IX_Sales_AuctionLotId ON Sales(AuctionLotId);
                CREATE INDEX IX_Sales_AuctionBidderId ON Sales(AuctionBidderId);
                CREATE INDEX IX_Sales_BidderNumber ON Sales(BidderNumber);
                INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','8')
                ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;
                """;
            migration.ExecuteNonQuery();
            transaction.Commit();
            version = 8;
        }
        if (version < 9)
        {
            using var transaction = connection.BeginTransaction();
            using var migration = connection.CreateCommand();
            migration.Transaction = transaction;
            migration.CommandText = """
                ALTER TABLE Auctions ADD COLUMN SalesTaxBasis TEXT NOT NULL DEFAULT 'HammerOnly';
                CREATE TABLE Invoices (
                    InvoiceId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuctionId INTEGER NOT NULL, AuctionBidderId INTEGER NOT NULL UNIQUE,
                    BuyerPremiumPercent TEXT NOT NULL, SalesTaxPercent TEXT NOT NULL, SalesTaxBasis TEXT NOT NULL,
                    HammerSubtotal TEXT NOT NULL DEFAULT '0', BuyerPremiumAmount TEXT NOT NULL DEFAULT '0',
                    SalesTaxAmount TEXT NOT NULL DEFAULT '0', TotalAmount TEXT NOT NULL DEFAULT '0',
                    Status TEXT NOT NULL DEFAULT 'Unpaid', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL,
                    FOREIGN KEY(AuctionId) REFERENCES Auctions(AuctionId) ON DELETE RESTRICT,
                    FOREIGN KEY(AuctionBidderId) REFERENCES AuctionBidders(AuctionBidderId) ON DELETE RESTRICT
                );
                CREATE INDEX IX_Invoices_AuctionId ON Invoices(AuctionId);
                CREATE INDEX IX_Invoices_Status ON Invoices(AuctionId,Status);
                INSERT INTO Invoices(AuctionId,AuctionBidderId,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,
                    HammerSubtotal,BuyerPremiumAmount,SalesTaxAmount,TotalAmount,Status,CreatedUtc,UpdatedUtc)
                SELECT ab.AuctionId,ab.AuctionBidderId,a.BuyerPremiumPercent,a.SalesTaxPercent,a.SalesTaxBasis,
                    '0','0','0','0','Unpaid',ab.RegisteredUtc,ab.UpdatedUtc
                FROM AuctionBidders ab JOIN Auctions a ON a.AuctionId=ab.AuctionId;
                UPDATE Invoices SET HammerSubtotal=CAST(ROUND((SELECT COALESCE(SUM(CAST(s.HammerPrice AS REAL)),0)
                    FROM Sales s WHERE s.AuctionBidderId=Invoices.AuctionBidderId),2) AS TEXT);
                UPDATE Invoices SET BuyerPremiumAmount=CAST(ROUND(CAST(HammerSubtotal AS REAL)*CAST(BuyerPremiumPercent AS REAL)/100.0,2) AS TEXT);
                UPDATE Invoices SET SalesTaxAmount=CAST(ROUND(CASE SalesTaxBasis
                    WHEN 'None' THEN 0
                    WHEN 'HammerOnly' THEN CAST(HammerSubtotal AS REAL)*CAST(SalesTaxPercent AS REAL)/100.0
                    ELSE (CAST(HammerSubtotal AS REAL)+CAST(BuyerPremiumAmount AS REAL))*CAST(SalesTaxPercent AS REAL)/100.0 END,2) AS TEXT);
                UPDATE Invoices SET TotalAmount=CAST(CAST(HammerSubtotal AS REAL)+CAST(BuyerPremiumAmount AS REAL)+CAST(SalesTaxAmount AS REAL) AS TEXT);
                CREATE TABLE Payments (
                    PaymentId INTEGER PRIMARY KEY AUTOINCREMENT, InvoiceId INTEGER NOT NULL,
                    Method TEXT NOT NULL, TenderedAmount TEXT NOT NULL, AppliedAmount TEXT NOT NULL,
                    ChangeDue TEXT NOT NULL DEFAULT '0', Reference TEXT NOT NULL DEFAULT '', CreatedUtc TEXT NOT NULL,
                    FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE RESTRICT
                );
                CREATE INDEX IX_Payments_InvoiceId ON Payments(InvoiceId);
                CREATE INDEX IX_Payments_CreatedUtc ON Payments(CreatedUtc);
                INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','9')
                ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;
                """;
            migration.ExecuteNonQuery();
            transaction.Commit();
            version = 9;
        }
        if(version<10)
        {
            using var transaction=connection.BeginTransaction();
            using var migration=connection.CreateCommand(); migration.Transaction=transaction;
            migration.CommandText="""
                CREATE TABLE ConsignorSettlements (
                    ConsignorSettlementId INTEGER PRIMARY KEY AUTOINCREMENT,
                    AuctionId INTEGER NOT NULL, ConsignorId INTEGER NOT NULL,
                    CommissionPercent TEXT NOT NULL, HammerSubtotal TEXT NOT NULL DEFAULT '0',
                    CommissionAmount TEXT NOT NULL DEFAULT '0', NetAmount TEXT NOT NULL DEFAULT '0',
                    Status TEXT NOT NULL DEFAULT 'Unpaid', CreatedUtc TEXT NOT NULL, UpdatedUtc TEXT NOT NULL,
                    UNIQUE(AuctionId,ConsignorId),
                    FOREIGN KEY(AuctionId) REFERENCES Auctions(AuctionId) ON DELETE RESTRICT,
                    FOREIGN KEY(ConsignorId) REFERENCES Consignors(ConsignorId) ON DELETE RESTRICT
                );
                CREATE INDEX IX_ConsignorSettlements_AuctionId ON ConsignorSettlements(AuctionId);
                CREATE INDEX IX_ConsignorSettlements_ConsignorId ON ConsignorSettlements(ConsignorId);
                CREATE INDEX IX_ConsignorSettlements_Status ON ConsignorSettlements(AuctionId,Status);
                CREATE TABLE ConsignorPayouts (
                    ConsignorPayoutId INTEGER PRIMARY KEY AUTOINCREMENT,
                    ConsignorSettlementId INTEGER NOT NULL, Method TEXT NOT NULL, Amount TEXT NOT NULL,
                    Reference TEXT NOT NULL DEFAULT '', CreatedUtc TEXT NOT NULL,
                    FOREIGN KEY(ConsignorSettlementId) REFERENCES ConsignorSettlements(ConsignorSettlementId) ON DELETE RESTRICT
                );
                CREATE INDEX IX_ConsignorPayouts_SettlementId ON ConsignorPayouts(ConsignorSettlementId);
                CREATE INDEX IX_ConsignorPayouts_CreatedUtc ON ConsignorPayouts(CreatedUtc);
                INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','10')
                ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;
                """;
            migration.ExecuteNonQuery(); transaction.Commit();
            version=10;
        }
        if(version<11){using var transaction=connection.BeginTransaction();using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="ALTER TABLE Consignors ADD COLUMN LinkedBuyerId INTEGER NULL REFERENCES Buyers(BuyerId) ON DELETE RESTRICT; CREATE INDEX IX_Consignors_LinkedBuyerId ON Consignors(LinkedBuyerId); INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','11') ON CONFLICT(Key) DO UPDATE SET Value='11';";migration.ExecuteNonQuery();transaction.Commit();}
        if(version<12){using var transaction=connection.BeginTransaction();using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="ALTER TABLE Auctions ADD COLUMN CreditCardFeePercent TEXT NOT NULL DEFAULT '2.65'; ALTER TABLE Invoices ADD COLUMN CreditCardFeeAmount TEXT NOT NULL DEFAULT '0'; ALTER TABLE Payments ADD COLUMN ProcessingFee TEXT NOT NULL DEFAULT '0'; INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','12') ON CONFLICT(Key) DO UPDATE SET Value='12';";migration.ExecuteNonQuery();transaction.Commit();}
        if(version<13){using var transaction=connection.BeginTransaction();using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="CREATE TABLE StaffPayments(StaffPaymentId INTEGER PRIMARY KEY AUTOINCREMENT,AuctionId INTEGER NOT NULL,Category TEXT NOT NULL,Payee TEXT NOT NULL,Amount TEXT NOT NULL,Method TEXT NOT NULL,PaidUtc TEXT NOT NULL,Notes TEXT NOT NULL DEFAULT '',VoidsStaffPaymentId INTEGER NULL,ReplacementForStaffPaymentId INTEGER NULL,VoidReason TEXT NOT NULL DEFAULT '',FOREIGN KEY(AuctionId) REFERENCES Auctions(AuctionId) ON DELETE RESTRICT,FOREIGN KEY(VoidsStaffPaymentId) REFERENCES StaffPayments(StaffPaymentId) ON DELETE RESTRICT,FOREIGN KEY(ReplacementForStaffPaymentId) REFERENCES StaffPayments(StaffPaymentId) ON DELETE RESTRICT);CREATE INDEX IX_StaffPayments_AuctionPaidUtc ON StaffPayments(AuctionId,PaidUtc);CREATE INDEX IX_StaffPayments_Voids ON StaffPayments(VoidsStaffPaymentId);INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','13') ON CONFLICT(Key) DO UPDATE SET Value='13';";migration.ExecuteNonQuery();transaction.Commit();}
        if(version<14){using var transaction=connection.BeginTransaction();using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="ALTER TABLE Auctions ADD COLUMN CreditCardFeeFixedAmount TEXT NOT NULL DEFAULT '0.15'; UPDATE Auctions SET CreditCardFeePercent='2.65'; INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','14') ON CONFLICT(Key) DO UPDATE SET Value='14';";migration.ExecuteNonQuery();transaction.Commit();}
        if(version<15){using var transaction=connection.BeginTransaction();using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="ALTER TABLE Auctions ADD COLUMN CompanyName TEXT NOT NULL DEFAULT ''; ALTER TABLE Auctions ADD COLUMN CompanyAddress TEXT NOT NULL DEFAULT ''; ALTER TABLE Auctions ADD COLUMN CompanyCity TEXT NOT NULL DEFAULT ''; ALTER TABLE Auctions ADD COLUMN CompanyState TEXT NOT NULL DEFAULT ''; ALTER TABLE Auctions ADD COLUMN CompanyZip TEXT NOT NULL DEFAULT ''; ALTER TABLE Auctions ADD COLUMN CompanyPhone TEXT NOT NULL DEFAULT ''; ALTER TABLE Auctions ADD COLUMN CompanyEmail TEXT NOT NULL DEFAULT ''; INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','15') ON CONFLICT(Key) DO UPDATE SET Value='15';";migration.ExecuteNonQuery();transaction.Commit();}
        if(version<16)
        {
            using var transaction=connection.BeginTransaction();
            if(!ColumnExists(connection,transaction,"Sales","Quantity")){using var alter=connection.CreateCommand();alter.Transaction=transaction;alter.CommandText="ALTER TABLE Sales ADD COLUMN Quantity INTEGER NOT NULL DEFAULT 1 CHECK(Quantity>0);";alter.ExecuteNonQuery();}
            using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','16') ON CONFLICT(Key) DO UPDATE SET Value='16';";migration.ExecuteNonQuery();transaction.Commit();
        }
        if(version<17)
        {
            using var transaction=connection.BeginTransaction();if(!ColumnExists(connection,transaction,"Sales","IsVoided")){using var alter=connection.CreateCommand();alter.Transaction=transaction;alter.CommandText="ALTER TABLE Sales ADD COLUMN IsVoided INTEGER NOT NULL DEFAULT 0; ALTER TABLE Sales ADD COLUMN VoidedUtc TEXT NOT NULL DEFAULT ''; ALTER TABLE Sales ADD COLUMN VoidReason TEXT NOT NULL DEFAULT ''; ALTER TABLE Sales ADD COLUMN CorrectionForSaleId INTEGER NULL REFERENCES Sales(Id) ON DELETE RESTRICT; CREATE INDEX IX_Sales_IsVoided ON Sales(AuctionId,IsVoided); CREATE INDEX IX_Sales_CorrectionFor ON Sales(CorrectionForSaleId);";alter.ExecuteNonQuery();}using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','17') ON CONFLICT(Key) DO UPDATE SET Value='17';";migration.ExecuteNonQuery();transaction.Commit();
        }
        if(version<18)
        {
            using var transaction=connection.BeginTransaction();using var migration=connection.CreateCommand();migration.Transaction=transaction;migration.CommandText="CREATE TABLE IF NOT EXISTS Refunds(RefundId INTEGER PRIMARY KEY AUTOINCREMENT,InvoiceId INTEGER NOT NULL,PaymentId INTEGER NOT NULL,Method TEXT NOT NULL,Amount TEXT NOT NULL,Reason TEXT NOT NULL,Reference TEXT NOT NULL DEFAULT '',CreatedUtc TEXT NOT NULL,FOREIGN KEY(InvoiceId) REFERENCES Invoices(InvoiceId) ON DELETE RESTRICT,FOREIGN KEY(PaymentId) REFERENCES Payments(PaymentId) ON DELETE RESTRICT); CREATE INDEX IF NOT EXISTS IX_Refunds_InvoiceId ON Refunds(InvoiceId); CREATE INDEX IF NOT EXISTS IX_Refunds_PaymentId ON Refunds(PaymentId); INSERT INTO Settings(Key,Value) VALUES('DatabaseSchemaVersion','18') ON CONFLICT(Key) DO UPDATE SET Value='18';";migration.ExecuteNonQuery();transaction.Commit();
        }
    }

    private static bool ColumnExists(SqliteConnection connection,SqliteTransaction transaction,string table,string column)
    {
        using var command=connection.CreateCommand();command.Transaction=transaction;command.CommandText=$"SELECT COUNT(*) FROM pragma_table_info('{table.Replace("'","''",StringComparison.Ordinal)}') WHERE name=$column;";command.Parameters.AddWithValue("$column",column);return Convert.ToInt32(command.ExecuteScalar(),CultureInfo.InvariantCulture)>0;
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

    private static void RefreshInvoiceTotals(SqliteConnection connection,SqliteTransaction transaction,long auctionBidderId)
    {
        decimal premiumRate; decimal taxRate; decimal cardFee; SalesTaxBasis taxBasis; long invoiceId;
        using (var command=connection.CreateCommand())
        {
            command.Transaction=transaction; command.CommandText="SELECT InvoiceId,BuyerPremiumPercent,SalesTaxPercent,SalesTaxBasis,CreditCardFeeAmount FROM Invoices WHERE AuctionBidderId=$registration;";
            command.Parameters.AddWithValue("$registration",auctionBidderId); using var reader=command.ExecuteReader();
            if(!reader.Read()) return;
            invoiceId=reader.GetInt64(0); premiumRate=decimal.Parse(reader.GetString(1),CultureInfo.InvariantCulture);
            taxRate=decimal.Parse(reader.GetString(2),CultureInfo.InvariantCulture); taxBasis=Enum.Parse<SalesTaxBasis>(reader.GetString(3));cardFee=decimal.Parse(reader.GetString(4),CultureInfo.InvariantCulture);
        }
        decimal hammer; decimal paid;
        using (var command=connection.CreateCommand())
        {
            command.Transaction=transaction; command.CommandText="SELECT COALESCE(SUM(CAST(HammerPrice AS REAL)*Quantity),0) FROM Sales WHERE AuctionBidderId=$registration AND IsVoided=0;";
            command.Parameters.AddWithValue("$registration",auctionBidderId); hammer=Convert.ToDecimal(command.ExecuteScalar(),CultureInfo.InvariantCulture);
        }
        using (var command=connection.CreateCommand())
        {
            command.Transaction=transaction; command.CommandText="SELECT COALESCE(SUM(CAST(AppliedAmount AS REAL)),0)-COALESCE((SELECT SUM(CAST(Amount AS REAL)) FROM Refunds WHERE InvoiceId=$invoice),0) FROM Payments WHERE InvoiceId=$invoice;";
            command.Parameters.AddWithValue("$invoice",invoiceId); paid=Convert.ToDecimal(command.ExecuteScalar(),CultureInfo.InvariantCulture);
        }
        hammer=decimal.Round(hammer,2,MidpointRounding.AwayFromZero);
        var premium=decimal.Round(hammer*premiumRate/100m,2,MidpointRounding.AwayFromZero);
        var taxable=taxBasis switch { SalesTaxBasis.None=>0m,SalesTaxBasis.HammerOnly=>hammer,SalesTaxBasis.HammerPlusBuyerPremium=>hammer+premium,_=>throw new InvalidOperationException("Invalid sales-tax basis.") };
        var tax=decimal.Round(taxable*taxRate/100m,2,MidpointRounding.AwayFromZero); var total=hammer+premium+tax+cardFee;
        var status=paid>=total && total>0 ? "Paid" : paid>0 ? "Partially Paid" : "Unpaid";
        using (var command=connection.CreateCommand())
        {
            command.Transaction=transaction; command.CommandText="""
                UPDATE Invoices SET HammerSubtotal=$hammer,BuyerPremiumAmount=$premium,SalesTaxAmount=$tax,TotalAmount=$total,Status=$status,UpdatedUtc=$updated WHERE InvoiceId=$invoice;
                UPDATE Sales SET Paid=$paid WHERE AuctionBidderId=$registration;
                """;
            command.Parameters.AddWithValue("$hammer",hammer.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$premium",premium.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$tax",tax.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$total",total.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$status",status); command.Parameters.AddWithValue("$updated",DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$invoice",invoiceId); command.Parameters.AddWithValue("$paid",status=="Paid"?1:0); command.Parameters.AddWithValue("$registration",auctionBidderId); command.ExecuteNonQuery();
        }
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
                AddLegacyBidderParameters(command, bidder);
                command.ExecuteNonQuery();

                var registration = new AuctionBidder
                {
                    AuctionId = GetCurrentAuctionId(connection), BidderNumber = bidder.BidderNumber, FirstName = bidder.FirstName,
                    LastName = bidder.LastName, DriversLicense = bidder.DriversLicense, Address = bidder.Address, City = bidder.City,
                    State = bidder.State, Zip = bidder.Zip, Phone = bidder.Phone, Email = bidder.Email, Notes = bidder.Notes
                };
                using var buyer = connection.CreateCommand();
                buyer.Transaction = transaction;
                buyer.CommandText = """
                    INSERT INTO Buyers(FirstName,LastName,DriversLicense,Address,City,State,Zip,Phone,Email,CreatedUtc,UpdatedUtc)
                    VALUES($first,$last,$license,$address,$city,$state,$zip,$phone,$email,$created,$updated); SELECT last_insert_rowid();
                    """;
                AddBuyerParameters(buyer, registration);
                var buyerId = Convert.ToInt64(buyer.ExecuteScalar(), CultureInfo.InvariantCulture);
                using var auctionBidder = connection.CreateCommand();
                auctionBidder.Transaction = transaction;
                auctionBidder.CommandText = """
                    INSERT OR IGNORE INTO AuctionBidders(AuctionId,BuyerId,BidderNumber,Notes,RegisteredUtc,UpdatedUtc)
                    VALUES($auctionId,$buyerId,$number,$notes,$created,$updated);
                    """;
                AddRegistrationParameters(auctionBidder, registration, buyerId);
                auctionBidder.ExecuteNonQuery();
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

                lot.AuctionId = GetCurrentAuctionId(connection);
                if (lot.ConsignorId <= 0)
                {
                    using var defaultConsignor = connection.CreateCommand();
                    defaultConsignor.Transaction = transaction;
                    defaultConsignor.CommandText = "SELECT MIN(ConsignorId) FROM Consignors;";
                    lot.ConsignorId = Convert.ToInt64(defaultConsignor.ExecuteScalar(), CultureInfo.InvariantCulture);
                }
                using var auctionLot = connection.CreateCommand();
                auctionLot.Transaction = transaction;
                auctionLot.CommandText = """
                    INSERT OR IGNORE INTO AuctionLots
                    (AuctionId, ConsignorId, LotNumber, Description, Category, StartingBid, Status, ReservePrice, PassOutMode, LotStatus, CreatedUtc, UpdatedUtc)
                    VALUES($auctionId, $consignorId, $number, $description, $category, $startingBid, $status, $reservePrice, $passOutMode, $lotStatus, $created, $updated);
                    """;
                AddLotParameters(auctionLot, lot);
                auctionLot.ExecuteNonQuery();
            }

            transaction.Commit();
            File.Move(_legacyJsonFile, _legacyJsonFile + ".migrated", true);
        }
    }

    private static List<AuctionBidder> LoadAuctionBidders(SqliteConnection connection, long auctionId)
    {
        var bidders = new List<AuctionBidder>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ab.AuctionBidderId,ab.AuctionId,ab.BuyerId,ab.BidderNumber,b.FirstName,b.LastName,b.DriversLicense,
                   b.Address,b.City,b.State,b.Zip,b.Phone,b.Email,ab.Notes
            FROM AuctionBidders ab JOIN Buyers b ON b.BuyerId=ab.BuyerId
            WHERE ab.AuctionId=$auctionId ORDER BY ab.BidderNumber;
            """;
        command.Parameters.AddWithValue("$auctionId", auctionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            bidders.Add(new AuctionBidder
            {
                AuctionBidderId=reader.GetInt64(0),AuctionId=reader.GetInt64(1),BuyerId=reader.GetInt64(2),BidderNumber=reader.GetInt32(3),
                FirstName=reader.GetString(4),LastName=reader.GetString(5),DriversLicense=reader.GetString(6),Address=reader.GetString(7),
                City=reader.GetString(8),State=reader.GetString(9),Zip=reader.GetString(10),Phone=reader.GetString(11),Email=reader.GetString(12),Notes=reader.GetString(13)
            });
        }
        return bidders;
    }

    private static List<AuctionLot> LoadAuctionLots(SqliteConnection connection, long auctionId)
    {
        var lots = new List<AuctionLot>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT AuctionLotId, LotNumber, Description, Category, StartingBid, Status, AuctionId, ConsignorId, ReservePrice, PassOutMode, LotStatus
            FROM AuctionLots WHERE AuctionId=$auctionId
            ORDER BY LotNumber;
            """;
        command.Parameters.AddWithValue("$auctionId", auctionId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            lots.Add(new AuctionLot
            {
                AuctionLotId = reader.GetInt64(0), LotNumber = reader.GetInt32(1), Description = reader.GetString(2), Category = reader.GetString(3),
                StartingBid = decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture), Status = reader.GetString(5), AuctionId = reader.GetInt64(6), ConsignorId = reader.GetInt64(7),
                ReservePrice = decimal.Parse(reader.GetString(8), CultureInfo.InvariantCulture), PassOutMode = reader.GetInt32(9) != 0, LotStatus = reader.GetString(10)
            });
        }
        return lots;
    }


    private static Sale ReadSale(SqliteDataReader reader)
    {
        return new Sale
        {
            Id = reader.GetInt64(0), AuctionId = reader.GetInt64(1), LotNumber = reader.GetInt32(2), BidderNumber = reader.GetInt32(3),
            HammerPrice = decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture), SaleUtc = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Paid = reader.GetInt32(6) != 0, Quantity = reader.GetInt32(7)
        };
    }

    private static void AddBuyerParameters(SqliteCommand command, AuctionBidder bidder)
    {
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        command.Parameters.AddWithValue("$first", bidder.FirstName ?? string.Empty);
        command.Parameters.AddWithValue("$last", bidder.LastName ?? string.Empty);
        command.Parameters.AddWithValue("$license", bidder.DriversLicense ?? string.Empty);
        command.Parameters.AddWithValue("$address", bidder.Address ?? string.Empty);
        command.Parameters.AddWithValue("$city", bidder.City ?? string.Empty);
        command.Parameters.AddWithValue("$state", bidder.State ?? string.Empty);
        command.Parameters.AddWithValue("$zip", bidder.Zip ?? string.Empty);
        command.Parameters.AddWithValue("$phone", bidder.Phone ?? string.Empty);
        command.Parameters.AddWithValue("$email", bidder.Email ?? string.Empty);
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
    }

    private static void AddLegacyBidderParameters(SqliteCommand command, AuctionBidder bidder)
    {
        AddBuyerParameters(command, bidder);
        command.Parameters.AddWithValue("$number", bidder.BidderNumber); command.Parameters.AddWithValue("$notes", bidder.Notes ?? string.Empty);
    }

    private static void AddRegistrationParameters(SqliteCommand command, AuctionBidder bidder, long buyerId)
    {
        var now = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        command.Parameters.AddWithValue("$auctionId", bidder.AuctionId); command.Parameters.AddWithValue("$buyerId", buyerId);
        command.Parameters.AddWithValue("$number", bidder.BidderNumber); command.Parameters.AddWithValue("$notes", bidder.Notes ?? string.Empty);
        command.Parameters.AddWithValue("$created", now); command.Parameters.AddWithValue("$updated", now);
    }

    private static void AddLotParameters(SqliteCommand command, AuctionLot lot)
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
        command.Parameters.AddWithValue("$taxExempt", consignor.TaxExempt ? 1 : 0); command.Parameters.AddWithValue("$required1099", consignor.Require1099 ? 1 : 0);
        command.Parameters.AddWithValue("$createdDate", (consignor.CreatedDate == default ? DateTime.UtcNow : consignor.CreatedDate).ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
    }

    private const string AuctionSelectSql = """
        SELECT AuctionId,AuctionNumber,AuctionName,AuctionDate,AuctionTime,Location,CompanyName,CompanyAddress,CompanyCity,CompanyState,CompanyZip,CompanyPhone,CompanyEmail,Auctioneer,Clerk,Cashier,
               BuyerPremiumPercent,CreditCardFeePercent,CreditCardFeeFixedAmount,DefaultConsignorCommissionPercent,SalesTaxPercent,SalesTaxBasis,InvoiceFooter,ReceiptFooter,
               Status,CreatedDate
        FROM Auctions
        """;

    private static long GetCurrentAuctionId(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CAST(Value AS INTEGER) FROM Settings WHERE Key='CurrentAuctionId' LIMIT 1;";
        var value = command.ExecuteScalar();
        if (value is not null && value != DBNull.Value) return Convert.ToInt64(value, CultureInfo.InvariantCulture);
        command.CommandText = "SELECT AuctionId FROM Auctions ORDER BY AuctionId LIMIT 1;";
        return Convert.ToInt64(command.ExecuteScalar() ?? throw new InvalidOperationException("No auction is configured."), CultureInfo.InvariantCulture);
    }

    private static Auction GetAuction(SqliteConnection connection, long auctionId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = AuctionSelectSql + " WHERE AuctionId=$id LIMIT 1;";
        command.Parameters.AddWithValue("$id", auctionId);
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new InvalidOperationException($"Auction #{auctionId} was not found.");
        return ReadAuction(reader);
    }

    private static Auction ReadAuction(SqliteDataReader reader) => new()
    {
        AuctionId = reader.GetInt64(0), AuctionNumber = reader.GetString(1), AuctionName = reader.GetString(2),
        AuctionDate = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
        AuctionTime = TimeSpan.Parse(reader.GetString(4), CultureInfo.InvariantCulture), Location = reader.GetString(5),CompanyName=reader.GetString(6),CompanyAddress=reader.GetString(7),CompanyCity=reader.GetString(8),CompanyState=reader.GetString(9),CompanyZip=reader.GetString(10),CompanyPhone=reader.GetString(11),CompanyEmail=reader.GetString(12),
        Auctioneer = reader.GetString(13), Clerk = reader.GetString(14), Cashier = reader.GetString(15),
        BuyerPremiumPercent = decimal.Parse(reader.GetString(16), CultureInfo.InvariantCulture),
        CreditCardFeePercent=decimal.Parse(reader.GetString(17),CultureInfo.InvariantCulture),CreditCardFeeFixedAmount=decimal.Parse(reader.GetString(18),CultureInfo.InvariantCulture),DefaultConsignorCommissionPercent = decimal.Parse(reader.GetString(19), CultureInfo.InvariantCulture),SalesTaxPercent=decimal.Parse(reader.GetString(20),CultureInfo.InvariantCulture), SalesTaxBasis=Enum.Parse<SalesTaxBasis>(reader.GetString(21)),InvoiceFooter=reader.GetString(22),ReceiptFooter=reader.GetString(23),Status=reader.GetString(24),CreatedDate=DateTime.Parse(reader.GetString(25),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind)
    };

    private static void AddAuctionParameters(SqliteCommand command, Auction auction, string now)
    {
        command.Parameters.AddWithValue("$id", auction.AuctionId); command.Parameters.AddWithValue("$number", auction.AuctionNumber.Trim());
        command.Parameters.AddWithValue("$name", auction.AuctionName.Trim()); command.Parameters.AddWithValue("$date", auction.AuctionDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$time", auction.AuctionTime.ToString("c", CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$location", auction.Location ?? string.Empty);
        command.Parameters.AddWithValue("$companyName",auction.CompanyName??string.Empty);command.Parameters.AddWithValue("$companyAddress",auction.CompanyAddress??string.Empty);command.Parameters.AddWithValue("$companyCity",auction.CompanyCity??string.Empty);command.Parameters.AddWithValue("$companyState",auction.CompanyState??string.Empty);command.Parameters.AddWithValue("$companyZip",auction.CompanyZip??string.Empty);command.Parameters.AddWithValue("$companyPhone",auction.CompanyPhone??string.Empty);command.Parameters.AddWithValue("$companyEmail",auction.CompanyEmail??string.Empty);
        command.Parameters.AddWithValue("$auctioneer", auction.Auctioneer ?? string.Empty); command.Parameters.AddWithValue("$clerk", auction.Clerk ?? string.Empty);
        command.Parameters.AddWithValue("$cashier", auction.Cashier ?? string.Empty); command.Parameters.AddWithValue("$premium", auction.BuyerPremiumPercent.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$cardFee",auction.CreditCardFeePercent.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$cardFixedFee",auction.CreditCardFeeFixedAmount.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$commission", auction.DefaultConsignorCommissionPercent.ToString(CultureInfo.InvariantCulture)); command.Parameters.AddWithValue("$tax", auction.SalesTaxPercent.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$taxBasis", auction.SalesTaxBasis.ToString());
        command.Parameters.AddWithValue("$invoiceFooter", auction.InvoiceFooter ?? string.Empty); command.Parameters.AddWithValue("$receiptFooter", auction.ReceiptFooter ?? string.Empty);
        command.Parameters.AddWithValue("$status", string.IsNullOrWhiteSpace(auction.Status) ? "Setup" : auction.Status); command.Parameters.AddWithValue("$created", now);
        command.Parameters.AddWithValue("$updated", now);
    }

    private static void SaveSetting(SqliteConnection connection, SqliteTransaction transaction, string key, string value)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO Settings(Key,Value) VALUES($key,$value) ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;";
        command.Parameters.AddWithValue("$key", key); command.Parameters.AddWithValue("$value", value); command.ExecuteNonQuery();
    }
}
