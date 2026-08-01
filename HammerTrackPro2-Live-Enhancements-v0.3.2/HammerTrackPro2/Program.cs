using HammerTrackPro2.Forms;
using HammerTrackPro2.Services;

namespace HammerTrackPro2;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var databasePath = DatabaseLocationService.ResolveDatabasePath();
            IDataStore dataStore = new SqliteDataStore(databasePath);
            IAuctionService auctionService = new AuctionService(dataStore);
            var consignorService = new ConsignorService((IConsignorRepository)dataStore);
            ICashierService cashierService = new CashierService(dataStore);
            Application.Run(new MainForm(auctionService, consignorService, cashierService));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"HammerTrack Pro could not start.\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
