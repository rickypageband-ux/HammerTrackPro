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
            if(!LicenseService.TryLoadValidLicense(out _,out var licenseError))
            {
                using var activation=new LicenseActivationForm(licenseError);
                if(activation.ShowDialog()!=DialogResult.OK)return;
            }
            if(!LicenseService.TryLoadValidLicense(out var installedLicense,out licenseError))throw new InvalidOperationException(licenseError);
            LicenseSession.Current=new LicenseAccess(installedLicense!);
            var configuration=WorkstationConfigurationService.Load();
            if(WorkstationConfigurationService.RequiresSetup(WorkstationConfigurationService.Exists,configuration))
            {
                using var setup=new WorkstationSetupForm(configuration);
                if(setup.ShowDialog()!=DialogResult.OK) return;
                configuration=setup.Configuration;
            }
            if(configuration.Role==WorkstationRole.Cashier)
            {
                using var remote=new RemoteCashierService(configuration);
                using var cashierForm=new CashierForm(new HammerTrackPro2.ViewModels.CashierViewModel(remote),remote,configuration);
                using var monitor=new LicenseMonitor(cashierForm);
                Application.Run(cashierForm);
                return;
            }
            WorkstationConfigurationService.Save(configuration);
            var databasePath = DatabaseLocationService.ResolveDatabasePath(configuration);
            IDataStore dataStore = new SqliteDataStore(databasePath);
            IAuctionService auctionService = new AuctionService(dataStore);
            var consignorService = new ConsignorService((IConsignorRepository)dataStore);
            ICashierService cashierService = new CashierService(dataStore);
            var networkHost=CashierNetworkHost.StartAsync(cashierService,configuration).GetAwaiter().GetResult();
            try { using var main=new MainForm(auctionService,consignorService,cashierService,configuration,new ModuleEntitlementService()); using var monitor=new LicenseMonitor(main); Application.Run(main); }
            finally { networkHost.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
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
