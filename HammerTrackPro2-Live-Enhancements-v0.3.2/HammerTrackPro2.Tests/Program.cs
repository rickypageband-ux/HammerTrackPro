using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using Microsoft.Data.Sqlite;

var tests = new (string Name, Action Run)[]
{
    ("Consignor MVVM CRUD/search", TestConsignorViewModel),
    ("Consignor commission validation", TestCommissionValidation),
    ("Version 5 migration and ownership persistence", TestMigrationAndPersistence)
};
foreach (var test in tests) { test.Run(); Console.WriteLine($"PASS {test.Name}"); }
Console.WriteLine($"{tests.Length} tests passed.");

static void TestConsignorViewModel()
{
    var repository=new FakeRepository(); var vm=new ConsignorViewModel(new ConsignorService(repository)); vm.Load(); vm.BeginNew();
    vm.Editor.ConsignorNumber=12; vm.Editor.BusinessName="Acme Farms"; vm.Editor.DefaultCommissionPercent=20; vm.Editor.CommissionOverridePercent=18; vm.Editor.PrintCheck=true; vm.Editor.HoldCheck=true; vm.Editor.TaxExempt=true; vm.Editor.Is1099Required=true;
    vm.Save(); Equal(1,repository.Items.Count); True(repository.Items[0].Is1099Required); vm.SearchText="Acme"; vm.Search(); Equal(1,vm.Consignors.Count);
    vm.BeginEdit(repository.Items[0]); vm.Editor.BusinessName="Acme Estate"; vm.Save(); Equal("Acme Estate",repository.Items[0].BusinessName); vm.BeginEdit(repository.Items[0]); vm.Delete(); Equal(0,repository.Items.Count);
}
static void TestCommissionValidation()
{
    var service=new ConsignorService(new FakeRepository()); var threw=false; try { service.SaveConsignor(new Consignor { ConsignorNumber=1, BusinessName="A", DefaultCommissionPercent=101 },null); } catch(InvalidOperationException) { threw=true; } True(threw);
}
static void TestMigrationAndPersistence()
{
    var folder=Path.Combine(Path.GetTempPath(),"HammerTrackTests",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder); var path=Path.Combine(folder,"test.db");
    try { var store=new SqliteDataStore(path); var auction=store.GetCurrentAuction(); True(auction.AuctionId>0); Equal(0m,auction.BuyerPremiumPercent);
        store.SaveConsignor(new Consignor { ConsignorNumber=22, BusinessName="Oak Hill", DefaultCommissionPercent=15, CommissionOverridePercent=12.5m, PrintCheck=true, HoldCheck=true, TaxExempt=true, Is1099Required=true, Active=true },null);
        var c=store.GetConsignors().Single(x=>x.ConsignorNumber==22); True(c.HoldCheck&&c.TaxExempt&&c.Is1099Required);
        store.SaveLot(new Lot { AuctionId=auction.AuctionId, ConsignorId=c.ConsignorId, LotNumber=44, Description="Clock", ReservePrice=200, PassOutMode=true, Status="Available", LotStatus="Available" },null);
        var lot=store.Load().Lots.Single(x=>x.LotNumber==44); Equal(c.ConsignorId,lot.ConsignorId); Equal(200m,lot.ReservePrice); True(lot.PassOutMode);
        using var db=new SqliteConnection($"Data Source={path}"); db.Open(); using var cmd=db.CreateCommand(); cmd.CommandText="SELECT Value FROM Settings WHERE Key='DatabaseSchemaVersion';"; Equal("5",cmd.ExecuteScalar()?.ToString()); }
    finally { try { Directory.Delete(folder,true); } catch { } }
}
static void True(bool value) { if(!value) throw new Exception("Assertion failed: expected true."); }
static void Equal<T>(T expected,T actual) { if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Assertion failed: expected {expected}, actual {actual}."); }
sealed class FakeRepository:IConsignorRepository
{
    public List<Consignor> Items {get;}=[]; public IReadOnlyList<Consignor> GetConsignors()=>Items;
    public void SaveConsignor(Consignor value,int? original) { if(original.HasValue) Items.RemoveAll(x=>x.ConsignorNumber==original); value.ConsignorId=value.ConsignorId==0?Items.Count+1:value.ConsignorId; Items.Add(value); }
    public void DeleteConsignor(int number)=>Items.RemoveAll(x=>x.ConsignorNumber==number);
}
