using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using Microsoft.Data.Sqlite;

namespace HammerTrackPro2.Tests;

[CollectionDefinition("License session", DisableParallelization = true)]
public sealed class LicenseSessionCollection { }

[Collection("License session")]
public sealed class DemoLicenseTests : IDisposable
{
    private static readonly DateTime Issued = new(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static LicensePayload Demo => new("demo-test", "Demo Customer", ["TEST-PC"], Issued, Issued.AddDays(7), "Demo");
    public void Dispose() => LicenseSession.Current = null;

    [Fact]
    public void LegacyPaidLicenseWithoutEditionStillVerifies()
    {
        using var rsa = RSA.Create(2048);
        // Independently serialize the original five-field contract, not the new record.
        var original = new { LicenseId = "old", CustomerName = "Old Customer", MachineIds = new[] { "TEST-PC" }, IssuedUtc = Issued, ExpiresUtc = (DateTime?)null };
        var payload = JsonSerializer.Serialize(original, JsonOptions);
        var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var json = JsonSerializer.Serialize(new { Payload = original, Signature = signature }, JsonOptions);
        Assert.True(LicenseService.ValidateDocument(json, "TEST-PC", rsa.ExportSubjectPublicKeyInfoPem(), Issued.AddYears(2), out var license, out _));
        Assert.Null(license!.Edition);
    }

    [Fact]
    public void SignedDemoHasExactSevenDayWindowAndCannotBeReimportedToRestart()
    {
        using var rsa = RSA.Create(2048); var json = Sign(rsa, Demo); var key = rsa.ExportSubjectPublicKeyInfoPem();
        Assert.True(LicenseService.ValidateDocument(json, "TEST-PC", key, Issued, out _, out _));
        Assert.True(LicenseService.ValidateDocument(json, "TEST-PC", key, Issued.AddDays(7).AddTicks(-1), out _, out _));
        Assert.False(LicenseService.ValidateDocument(json, "TEST-PC", key, Issued.AddDays(7), out _, out _));
        Assert.False(LicenseService.ValidateDocument(json, "TEST-PC", key, Issued.AddDays(8), out _, out _));
        Assert.False(LicenseService.ValidateDocument(json, "TEST-PC", key, Issued.AddTicks(-1), out _, out _));
        Assert.False(LicenseService.ValidateDocument(json, "OTHER-PC", key, Issued, out _, out _));
        Assert.False(LicenseService.ValidateDocument(json.Replace("Demo Customer", "Other"), "TEST-PC", key, Issued, out _, out _));
        Assert.False(LicenseService.ValidateDocument(json.Replace("\"Edition\": \"Demo\"", "\"Edition\": null"), "TEST-PC", key, Issued, out _, out _));
    }

    [Fact]
    public void DemoCannotHaveMissingOrExtendedExpiryEvenWithValidSignature()
    {
        using var rsa = RSA.Create(2048);
        foreach (var payload in new[] { Demo with { ExpiresUtc = null }, Demo with { ExpiresUtc = Issued.AddDays(8) }, Demo with { Edition = "Unknown" } })
            Assert.False(LicenseService.ValidateDocument(Sign(rsa, payload), "TEST-PC", rsa.ExportSubjectPublicKeyInfoPem(), Issued, out _, out _));
    }

    [Fact]
    public void DemoBlocksBothScannerProcessingAndModuleActivationThenPaidUnlocksScanner()
    {
        var access = new LicenseAccess(Demo, () => Issued.AddDays(1)); LicenseSession.Current = access;
        var vm = new LicenseViewModel(access);
        Assert.Contains("SEVEN-DAY DEMO", vm.Status);
        Assert.False(vm.CanUseFeature("Scanner", out _));
        Assert.Throws<InvalidOperationException>(() => new HandheldLicenseScanViewModel().Receive("barcode"));
        using var bitmap = new System.Drawing.Bitmap(10, 10);
        Assert.Throws<InvalidOperationException>(() => new DriverLicenseScanViewModel(new DriverLicenseBarcodeDecoder()).ProcessFrame(bitmap));
        var modules = new ModuleEntitlementService();
        Assert.False(modules.IsActivated(ProductModules.Wholesale, out var reason)); Assert.Contains("demo", reason);
        Assert.Throws<InvalidOperationException>(() => modules.Activate("anything", ProductModules.Wholesale));
        access.Replace(Demo with { Edition = null, ExpiresUtc = null });
        Assert.True(vm.CanUseFeature("Scanner", out _));
        new HandheldLicenseScanViewModel().Receive("barcode");
        Assert.Contains("Licensed to", vm.Status);
    }

    [Fact]
    public void FullRetailAuctionPersistsThroughExpiryAndPaidUpgrade()
    {
        var folder = Path.Combine(Path.GetTempPath(), "HammerTrackDemoTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var now = Issued.AddDays(1); var access = new LicenseAccess(Demo, () => now); LicenseSession.Current = access;
        try
        {
            var path = Path.Combine(folder, "auction.db");
            var store = new SqliteDataStore(path); var service = new AuctionService(store); var cashier = new CashierService(store);
            var auction = service.GetCurrentAuction();
            auction.BuyerPremiumPercent = 0; auction.SalesTaxPercent = 0; auction.DefaultConsignorCommissionPercent = 0;
            service.SaveAuction(auction);
            cashier.RegisterBuyer(new AuctionBidder { BidderNumber = 101, FirstName = "Trial", DriversLicense = "MANUAL-ENTRY" });
            var seller = store.GetConsignors().First();
            var lot = new AuctionLot { AuctionId = auction.AuctionId, ConsignorId = seller.ConsignorId, LotNumber = 1, Description = "Towels" };
            service.SaveLot(lot, null);
            lot.SaleMode = AuctionSaleMode.Wholesale;
            Assert.Throws<InvalidOperationException>(() => service.SaveLot(lot, 1));
            lot.SaleMode = AuctionSaleMode.Retail;
            var sale = service.RecordSale(1, 101, 20m);
            var invoice = cashier.LoadInvoice(101);
            cashier.RecordPayments(101, [new PaymentRequest { Method = PaymentMethod.Cash, Amount = invoice.BalanceDue }]);
            var settlement = cashier.LoadConsignorSettlement(seller.ConsignorNumber);
            Assert.Equal(20m, settlement.HammerSubtotal);
            Assert.True(File.Exists(service.Backup()));
            now = Issued.AddDays(7);
            Assert.False(new LicenseViewModel(access).IsActive);
            Assert.Throws<InvalidOperationException>(() => cashier.RegisterBuyer(new AuctionBidder { BidderNumber = 102 }));
            Assert.Throws<InvalidOperationException>(() => store.GetCurrentAuction());
            access.Replace(Demo with { Edition = null, ExpiresUtc = null });
            var reopened = new SqliteDataStore(path);
            Assert.Equal(sale.Id, Assert.Single(reopened.GetSalesForLot(1)).Id);
            Assert.Equal(0m, new CashierService(reopened).LoadInvoice(101).BalanceDue);
            Assert.DoesNotContain(reopened.Load().Bidders, x => x.BidderNumber == 102);
            Assert.Equal("MANUAL-ENTRY", reopened.Load().Bidders.Single(x => x.BidderNumber == 101).DriversLicense);
        }
        finally { LicenseSession.Current = null; SqliteConnection.ClearAllPools(); Directory.Delete(folder, true); }
    }

    [Fact]
    public void ImportPreservesOldLicenseOnFailureAndReplacesDemoWithPaid()
    {
        var folder=Path.Combine(Path.GetTempPath(),"HammerTrackDemoImport",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            using var rsa=RSA.Create(2048);var key=rsa.ExportSubjectPublicKeyInfoPem();
            var source=Path.Combine(folder,"incoming.json");var target=Path.Combine(folder,"installed.json");
            var demo=Sign(rsa,Demo);File.WriteAllText(source,demo);
            Assert.Equal("Demo",LicenseService.InstallLicenseFile(source,target,"TEST-PC",key,Issued).Edition);
            Assert.Throws<InvalidOperationException>(()=>LicenseService.InstallLicenseFile(source,target,"TEST-PC",key,Issued.AddDays(7)));
            Assert.Equal(demo,File.ReadAllText(target));
            File.WriteAllText(source,Sign(rsa,Demo with { Edition=null,ExpiresUtc=null }));
            Assert.Throws<InvalidOperationException>(()=>LicenseService.InstallLicenseFile(source,target,"WRONG-PC",key,Issued));
            Assert.Equal(demo,File.ReadAllText(target));
            Assert.Null(LicenseService.InstallLicenseFile(source,target,"TEST-PC",key,Issued.AddDays(8)).Edition);
            Assert.True(LicenseService.ValidateDocument(File.ReadAllText(target),"TEST-PC",key,Issued.AddYears(1),out var paid,out _));
            Assert.Null(paid!.Edition);Assert.False(File.Exists(target+".new"));
        }
        finally { Directory.Delete(folder,true); }
    }

    [Fact]
    public async Task ExpiredHostRejectsPairedCashierRequests()
    {
        var folder=Path.Combine(Path.GetTempPath(),"HammerTrackDemoNetwork",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var now=Issued.AddDays(1);LicenseSession.Current=new LicenseAccess(Demo,()=>now);
        try
        {
            var store=new SqliteDataStore(Path.Combine(folder,"auction.db"));
            var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();
            var port=((System.Net.IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
            var configuration=new WorkstationConfiguration { ServicePort=port, PairingKey="TEST-PAIRING-KEY" };
            await using var host=await CashierNetworkHost.StartAsync(new CashierService(store),configuration,useHttps:false);
            using var client=new HttpClient { BaseAddress=new Uri($"http://localhost:{port}") };
            client.DefaultRequestHeaders.Add("X-HammerTrack-Key",configuration.PairingKey);
            using var valid=await client.GetAsync("/api/current-auction");Assert.True(valid.IsSuccessStatusCode);
            now=Issued.AddDays(7);
            using var expired=await client.GetAsync("/api/current-auction");Assert.Equal(System.Net.HttpStatusCode.BadRequest,expired.StatusCode);
            Assert.Contains("expired",await expired.Content.ReadAsStringAsync());
        }
        finally { LicenseSession.Current=null;SqliteConnection.ClearAllPools();Directory.Delete(folder,true); }
    }

    [Fact]
    public void DemoStatusAndUpgradeActionRemainVisibleBesideDockedContent()
    {
        LicenseSession.Current=new LicenseAccess(Demo,()=>Issued.AddDays(1));
        Exception? failure=null;
        var thread=new Thread(()=>
        {
            try
            {
                using var form=new System.Windows.Forms.Form { Width=1100,Height=700,ShowInTaskbar=false,StartPosition=System.Windows.Forms.FormStartPosition.Manual,Location=new System.Drawing.Point(-25000,-25000) };
                var content=new System.Windows.Forms.Panel { Dock=System.Windows.Forms.DockStyle.Fill };form.Controls.Add(content);
                using var monitor=new HammerTrackPro2.Forms.LicenseMonitor(form);
                form.Show();System.Windows.Forms.Application.DoEvents();
                var strip=Assert.Single(form.Controls.OfType<System.Windows.Forms.StatusStrip>());
                Assert.True(strip.Visible);Assert.True(strip.Height>=30);
                Assert.True(content.Bottom<=strip.Top);
                Assert.Contains(strip.Items.Cast<System.Windows.Forms.ToolStripItem>(),x=>x.Text?.Contains("SEVEN-DAY DEMO")==true);
                Assert.Contains(strip.Items.Cast<System.Windows.Forms.ToolStripItem>(),x=>x.Text=="Import / Upgrade License");
                form.Close();
            }
            catch(Exception exception) { failure=exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();
        if(failure is not null)throw new InvalidOperationException("Demo status layout failed",failure);
    }

    private static string Sign(RSA rsa, LicensePayload payload)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(json), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return JsonSerializer.Serialize(new LicenseDocument(payload, Convert.ToBase64String(signature)), JsonOptions);
    }
}
