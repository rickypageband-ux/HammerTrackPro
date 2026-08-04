using System.Security.Cryptography;
using System.Text;
using HammerTrackPro2.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HammerTrackPro2.Services;

public sealed class CashierNetworkHost : IAsyncDisposable
{
    private readonly WebApplication _application;
    private CashierNetworkHost(WebApplication application) => _application=application;

    public static async Task<CashierNetworkHost> StartAsync(ICashierService cashier,WorkstationConfiguration configuration,CancellationToken cancellationToken=default,System.Security.Cryptography.X509Certificates.X509Certificate2? certificateOverride=null,bool useHttps=true)
    {
        var builder=WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options=>
        {
            if(useHttps)
            {
                var certificate=certificateOverride ?? NetworkCertificateService.GetOrCreate(configuration);
                options.ListenAnyIP(configuration.ServicePort,listen=>listen.UseHttps(certificate));
            }
            else options.ListenLocalhost(configuration.ServicePort);
        });
        builder.Services.AddSingleton(cashier);
        var application=builder.Build();
        application.Use(async(context,next)=>
        {
            var supplied=context.Request.Headers["X-HammerTrack-Key"].ToString();
            var expected=configuration.PairingKey;
            if(string.IsNullOrEmpty(supplied)||!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied),Encoding.UTF8.GetBytes(expected)))
            { context.Response.StatusCode=StatusCodes.Status401Unauthorized; await context.Response.WriteAsJsonAsync(new { detail="This workstation is not paired with the auction host." },cancellationToken); return; }
            await next();
        });
        application.MapGet("/api/health",()=>Results.Ok(new { status="ok",machine=Environment.MachineName }));
        application.MapGet("/api/current-auction",(ICashierService service)=>Execute(()=>service.GetCurrentAuction()));
        application.MapGet("/api/invoices/{bidderNumber:int}",(int bidderNumber,ICashierService service)=>Execute(()=>service.LoadInvoice(bidderNumber)));
        application.MapGet("/api/invoices/unpaid",(ICashierService service)=>Execute(()=>service.GetUnpaidInvoices()));
        application.MapPost("/api/registrations/buyers",(AuctionBidder bidder,ICashierService service)=>Execute(()=>service.RegisterBuyer(bidder)));
        application.MapPost("/api/invoices/{bidderNumber:int}/payments",(int bidderNumber,List<PaymentRequest> payments,ICashierService service)=>Execute(()=>service.RecordPayments(bidderNumber,payments)));
        application.MapGet("/api/settlements/{consignorNumber:int}",(int consignorNumber,ICashierService service)=>Execute(()=>service.LoadConsignorSettlement(consignorNumber)));
        application.MapPost("/api/registrations/consignors",(Consignor consignor,ICashierService service)=>Execute(()=>service.RegisterConsignor(consignor)));
        application.MapPost("/api/settlements/{consignorNumber:int}/payouts",(int consignorNumber,ConsignorPayoutRequest payout,ICashierService service)=>Execute(()=>service.RecordConsignorPayout(consignorNumber,payout)));
        application.MapGet("/api/auction-settlement",(ICashierService service)=>Execute(()=>service.GetAuctionSettlementReport()));
        application.MapPost("/api/staff-payments",(StaffPayment payment,ICashierService service)=>Execute(()=>service.RecordStaffPayment(payment)));
        application.MapPost("/api/staff-payments/{id:long}/void",(long id,StaffPaymentVoidRequest request,ICashierService service)=>Execute(()=>service.VoidStaffPayment(id,request.Reason,request.Replacement)));
        await application.StartAsync(cancellationToken); return new CashierNetworkHost(application);
    }

    private static IResult Execute<T>(Func<T> operation)
    {
        try { return Results.Ok(operation()); }
        catch(InvalidOperationException exception) { return Results.Problem(exception.Message,statusCode:400); }
    }
    public async ValueTask DisposeAsync() { await _application.StopAsync(); await _application.DisposeAsync(); }
}
public sealed class StaffPaymentVoidRequest{public string Reason{get;set;}=string.Empty;public StaffPayment? Replacement{get;set;}}
