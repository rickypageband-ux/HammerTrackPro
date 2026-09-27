using HammerTrackPro2.Forms;
using HammerTrackPro2.Models;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;

Application.EnableVisualStyles();
Application.SetCompatibleTextRenderingDefault(false);
var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
var output=Path.Combine(root,"output","manual-assets");Directory.CreateDirectory(output);
var dataFolder=Path.Combine(root,"tmp","manual-capture-data",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dataFolder);var db=Path.Combine(dataFolder,"manual.db");
var store=new SqliteDataStore(db);var auctionService=new AuctionService(store);var sellers=new ConsignorService(store);var cashier=new CashierService(store);
var auction=auctionService.GetCurrentAuction();auction.AuctionName="Sample Saturday Auction";auction.AuctionNumber="SAT-001";auction.CompanyName="Sample Auction Company";auction.CompanyAddress="123 Auction Lane";auction.CompanyCity="Columbia";auction.CompanyState="SC";auction.CompanyZip="29201";auction.CompanyPhone="(555) 555-0100";auction.CompanyEmail="office@example.com";auction.BuyerPremiumPercent=10;auction.SalesTaxPercent=7;auction.DefaultConsignorCommissionPercent=20;auction.Status="Live";auctionService.SaveAuction(auction);
sellers.SaveConsignor(new Consignor{ConsignorNumber=75,BusinessName="Sample Estate",FirstName="Sarah",LastName="Seller",Active=true,DefaultCommissionPercent=20},null);
var consignor=sellers.GetByNumber(75);
for(var bidder=101;bidder<=103;bidder++)auctionService.SaveBidder(new AuctionBidder{AuctionId=auction.AuctionId,BidderNumber=bidder,FirstName=$"Buyer {bidder}",LastName="Sample",Address="100 Main Street",City="Columbia",State="SC",Zip="29201",Phone="(555) 555-0123",Email="buyer@example.com"},null);
var retailVm=new LiveAuctionViewModel(auctionService,sellers,AuctionSaleMode.Retail);retailVm.QuickAddLot(75,"Antique table lamp");
auctionService.RecordSale(1,101,25m,false,2);
retailVm.QuickAddLot(75,"Oak dining chair");

void Capture(Form form,string file,int width=1600,int height=900,Action<Form>? prepare=null)
{
    form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-25000,-25000);form.Size=new Size(width,height);form.ShowInTaskbar=false;form.Show();Application.DoEvents();prepare?.Invoke(form);Application.DoEvents();
    foreach(var control in Descendants(form))if(control.Text.Contains(LicenseService.GetMachineId()))control.Text=control.Text.Replace(LicenseService.GetMachineId(),"SAMPLE-MACHINE-ID");
    using var bitmap=new Bitmap(form.Width,form.Height);form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine(output,file));form.Close();Application.DoEvents();
}

Capture(new MainForm(auctionService,sellers,cashier,new WorkstationConfiguration{Role=WorkstationRole.Host},new AlwaysActivated(),new NoOpManual()),"dashboard.png");
Capture(new MainForm(auctionService,sellers,cashier,new WorkstationConfiguration{Role=WorkstationRole.Host},new AlwaysActivated(),new NoOpManual()),"bidder-registration.png",1600,1050,form=>form.Controls.Cast<Control>().SelectMany(Descendants).OfType<TabControl>().First().SelectedIndex=1);
Capture(new MainForm(auctionService,sellers,cashier,new WorkstationConfiguration{Role=WorkstationRole.Host},new AlwaysActivated(),new NoOpManual()),"lots.png",1600,1000,form=>form.Controls.Cast<Control>().SelectMany(Descendants).OfType<TabControl>().First().SelectedIndex=2);
Capture(new AuctionSetupForm(new AuctionSetupViewModel(auctionService)),"auction-setup.png",1500,900);
Capture(new ConsignorForm(sellers),"seller-management.png",1500,900);
Capture(new LiveAuctionForm(auctionService,retailVm),"retail-auction.png",1900,1200);

var wholesaleVm=new LiveAuctionViewModel(auctionService,sellers,AuctionSaleMode.Wholesale);var wholesaleLot=wholesaleVm.QuickAddLot(75,"Case of collectible glassware");
auctionService.RecordSale(wholesaleLot.LotNumber,101,5m,true,4,6);
auctionService.RecordSale(wholesaleLot.LotNumber,102,5m,true,4,2);
Capture(new LiveAuctionForm(auctionService,wholesaleVm),"wholesale-auction.png",1900,1000,form=>{
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    ((CheckBox)form.GetType().GetField("_passOutMode",flags)!.GetValue(form)!).Checked=false;
    foreach(var entry in new[]{("_hammerPrice",5m),("_quantity",24m),("_bidderNumber",103m)})
        ((NumericUpDown)form.GetType().GetField(entry.Item1,flags)!.GetValue(form)!).Value=entry.Item2;
});
Capture(new LiveAuctionForm(auctionService,wholesaleVm),"wholesale-pass-out.png",1900,1250,form=>{
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    foreach(var entry in new[]{("_lotNumber",(decimal)wholesaleLot.LotNumber),("_hammerPrice",5m),("_quantity",4m),("_deals",6m),("_bidderNumber",103m)})
        ((NumericUpDown)form.GetType().GetField(entry.Item1,flags)!.GetValue(form)!).Value=entry.Item2;
    ((CheckBox)form.GetType().GetField("_passOutMode",flags)!.GetValue(form)!).Checked=true;
});
var cashierVm=new CashierViewModel(cashier);cashierVm.Lookup(101);
Capture(new CashierForm(cashierVm,cashier),"cashier.png",2000,1100,form=>{
    var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    ((NumericUpDown)form.GetType().GetField("_bidderNumber",flags)!.GetValue(form)!).Value=101;
    form.GetType().GetMethod("RefreshInvoice",flags)!.Invoke(form,null);
});
Capture(new HandheldLicenseScannerForm(),"license-scanner.png",800,460);
Capture(new CashierBidderSearchForm(new CashierBidderSearchViewModel(cashier.GetRegisteredBidders())),"buyer-search.png",850,580);
Capture(new PrinterSetupForm(new PrinterSetupViewModel(new SamplePrinter(),new SamplePrinter())),"printer-setup.png",700,410);
Capture(new SoldItemSearchForm(new SoldItemSearchViewModel(auctionService,AuctionSaleMode.Retail)),"sold-item-search.png",1100,760);
Capture(new AuctionSettlementForm(cashier),"end-of-night.png",1600,900);
Capture(new WholesaleActivationForm(new WholesaleActivationViewModel(new AlwaysActivated()),"Wholesale Auction is not activated on this computer."),"wholesale-activation.png",850,560);
Capture(new LicenseActivationForm("No HammerTrack Pro license is installed."),"license-activation.png",760,420);

static IEnumerable<Control> Descendants(Control parent)
{
    yield return parent;
    foreach(Control child in parent.Controls)foreach(var descendant in Descendants(child))yield return descendant;
}

sealed class AlwaysActivated : IModuleEntitlementService
{
    public bool IsActivated(string module,out string reason){reason=string.Empty;return true;}
    public ModuleActivationPayload Activate(string key,string module)=>new("sample",module,"Sample Auction Company",[LicenseService.GetMachineId()],DateTime.UtcNow,null);
}
sealed class NoOpManual : IUserManualService
{
    public string ResolveManualPath()=>string.Empty;
    public void OpenManual(){}
}

sealed class SamplePrinter : IPrinterCatalog,IPrinterPreferenceStore
{
    public IReadOnlyList<string> GetInstalledPrinters()=>new[]{"Sample Office Printer"};
    public string GetDefaultPrinter()=>"Sample Office Printer";
    public string PrinterName=>"Sample Office Printer";
    public void Save(string name){}
}
