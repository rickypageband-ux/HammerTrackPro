using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Tests;

public sealed class NetworkCashierTests
{
    [Fact]
    public async Task CashierClient_LoadsInvoiceAndRecordsPaymentThroughHost()
    {
        using var database=TemporaryDatabase.Create(); var store=CreateAccount(database.Path); var local=new CashierService(store);
        var configuration=Configuration("PAIRING-KEY");
        await using var host=await CashierNetworkHost.StartAsync(local,configuration,useHttps:false);
        using var remote=new RemoteCashierService(configuration,true);

        Assert.True(await remote.CheckConnectionAsync());
        var invoice=remote.LoadInvoice(25); Assert.Equal(100m,invoice.HammerSubtotal);
        var paid=remote.RecordPayments(25,[new PaymentRequest { Method=PaymentMethod.Cash,Amount=120 }]);
        Assert.Equal("Paid",paid.Status); Assert.Equal(10m,paid.ChangeTotal());
    }

    [Fact]
    public async Task CashierClient_RejectsAnUnpairedWorkstation()
    {
        using var database=TemporaryDatabase.Create(); var store=CreateAccount(database.Path);
        var hostConfiguration=Configuration("RIGHT-KEY");
        await using var host=await CashierNetworkHost.StartAsync(new CashierService(store),hostConfiguration,useHttps:false);
        var clientConfiguration=new WorkstationConfiguration { Role=WorkstationRole.Cashier,HostUrl=hostConfiguration.HostUrl,ServicePort=hostConfiguration.ServicePort,PairingKey="WRONG-KEY" };
        using var remote=new RemoteCashierService(clientConfiguration,true);
        Assert.False(await remote.CheckConnectionAsync());
        Assert.Throws<InvalidOperationException>(()=>remote.LoadInvoice(25));
    }

    [Fact]
    public async Task CashierClient_RecordsClosedAuctionConsignorPayoutThroughHost()
    {
        using var database=TemporaryDatabase.Create(); var store=CreateAccount(database.Path); var consignor=store.GetConsignors().First(); store.CloseAuction(store.GetCurrentAuction().AuctionId);
        var configuration=Configuration("SETTLEMENT-KEY"); await using var host=await CashierNetworkHost.StartAsync(new CashierService(store),configuration,useHttps:false); using var remote=new RemoteCashierService(configuration,true);
        var preview=remote.LoadConsignorSettlement(consignor.ConsignorNumber); Assert.Equal(100m,preview.NetAmount);
        var paid=remote.RecordConsignorPayout(consignor.ConsignorNumber,new ConsignorPayoutRequest { Method=ConsignorPayoutMethod.Ach,Amount=100,Reference="ACH-NET-1" });
        Assert.Equal("Paid",paid.Status); Assert.Equal(0,paid.BalanceDue);
    }

    [Fact]
    public void HostConfiguration_RejectsANetworkDatabasePath()
    {
        var configuration=new WorkstationConfiguration { Role=WorkstationRole.Host,DatabasePath=@"\\server\auction\hammertrack.db" };
        var exception=Assert.Throws<InvalidOperationException>(()=>DatabaseLocationService.ResolveDatabasePath(configuration));
        Assert.Contains("local drive",exception.Message);
    }

    [Fact]
    public void ProductionCashier_RejectsAnUnencryptedHostAddress()
    {
        var configuration=new WorkstationConfiguration { Role=WorkstationRole.Cashier,HostUrl="http://auction-pc:7443",PairingKey="key",CertificateThumbprint="AA" };
        Assert.Throws<InvalidOperationException>(()=>new RemoteCashierService(configuration));
    }

    private static WorkstationConfiguration Configuration(string key)
    {
        var port=FreePort(); return new WorkstationConfiguration { Role=WorkstationRole.Cashier,HostUrl=$"http://localhost:{port}",ServicePort=port,PairingKey=key };
    }
    private static int FreePort() { var listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); var port=((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
    private static SqliteDataStore CreateAccount(string path)
    {
        var store=new SqliteDataStore(path); var auction=store.GetCurrentAuction(); auction.AuctionNumber="NET-1"; auction.BuyerPremiumPercent=10; auction.SalesTaxBasis=SalesTaxBasis.None; store.SaveAuction(auction);
        store.SaveBidder(new AuctionBidder { AuctionId=auction.AuctionId,BidderNumber=25,FirstName="Network",LastName="Buyer" },null);
        var consignor=store.GetConsignors().First(); store.SaveLot(new AuctionLot { AuctionId=auction.AuctionId,ConsignorId=consignor.ConsignorId,LotNumber=1,Description="Network Lot" },null); store.RecordSale(1,25,100); return store;
    }
    private sealed class TemporaryDatabase : IDisposable
    {
        private readonly string _folder; private TemporaryDatabase(string folder) { _folder=folder; Path=System.IO.Path.Combine(folder,"test.db"); }
        public string Path { get; }
        public static TemporaryDatabase Create() { var folder=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"HammerTrackPro2.Tests",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); return new(folder); }
        public void Dispose() { SqliteConnection.ClearAllPools(); Directory.Delete(_folder,true); }
    }
}

internal static class InvoiceTestExtensions
{
    public static decimal ChangeTotal(this Invoice invoice)=>invoice.Payments.Sum(item=>item.ChangeDue);
}
