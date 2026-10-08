using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HammerTrackPro2.Models;

namespace HammerTrackPro2.Services;

public sealed class RemoteCashierService : ICashierService,IDisposable
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions=new(JsonSerializerDefaults.Web);
    public RemoteCashierService(WorkstationConfiguration configuration) : this(configuration,false) { }
    internal RemoteCashierService(WorkstationConfiguration configuration,bool allowInsecureLoopback)
    {
        if(!Uri.TryCreate(configuration.HostUrl,UriKind.Absolute,out var host)||(host.Scheme!=Uri.UriSchemeHttps && !(allowInsecureLoopback && host.Scheme==Uri.UriSchemeHttp && host.IsLoopback)))
            throw new InvalidOperationException("Enter a valid HTTPS host address, for example https://AUCTION-PC:7443.");
        var expected=Normalize(configuration.CertificateThumbprint);
        if(host.Scheme==Uri.UriSchemeHttps && expected.Length==0) throw new InvalidOperationException("Enter the certificate fingerprint shown on the live-auction PC.");
        var handler=new HttpClientHandler();
        if(host.Scheme==Uri.UriSchemeHttps) handler.ServerCertificateCustomValidationCallback=(_,certificate,_,_)=>certificate is not null && Normalize(certificate.GetCertHashString())==expected;
        _client=new HttpClient(handler) { BaseAddress=new Uri(host.GetLeftPart(UriPartial.Authority)),Timeout=TimeSpan.FromSeconds(8) };
        _client.DefaultRequestHeaders.Add("X-HammerTrack-Key",configuration.PairingKey);
    }
    public Auction GetCurrentAuction()=>Get<Auction>("/api/current-auction");
    public Invoice LoadInvoice(int bidderNumber)=>Get<Invoice>($"/api/invoices/{bidderNumber}");
    public Invoice RegisterBuyer(AuctionBidder bidder)=>Post<AuctionBidder,Invoice>("/api/registrations/buyers",bidder);
    public IReadOnlyList<RegisteredBidderSummary> GetRegisteredBidders()=>Get<List<RegisteredBidderSummary>>("/api/registrations/buyers");
    public ConsignorSettlement LoadConsignorSettlement(int consignorNumber)=>Get<ConsignorSettlement>($"/api/settlements/{consignorNumber}");
    public ConsignorSettlement RegisterConsignor(Consignor consignor)=>Post<Consignor,ConsignorSettlement>("/api/registrations/consignors",consignor);
    public IReadOnlyList<UnpaidInvoiceSummary> GetUnpaidInvoices()=>Get<List<UnpaidInvoiceSummary>>("/api/invoices/unpaid");
    public AuctionSettlementReport GetAuctionSettlementReport()=>Get<AuctionSettlementReport>("/api/auction-settlement");
    public StaffPayment RecordStaffPayment(StaffPayment payment)=>Post<StaffPayment,StaffPayment>("/api/staff-payments",payment);
    public StaffPayment VoidStaffPayment(long id,string reason,StaffPayment? replacement)=>Post<object,StaffPayment>($"/api/staff-payments/{id}/void",new{reason,replacement});
    public ConsignorSettlement RecordConsignorPayout(int consignorNumber,ConsignorPayoutRequest payout)
    {
        LicenseSession.Current?.EnsureActive();
        try { using var response=_client.PostAsJsonAsync($"/api/settlements/{consignorNumber}/payouts",payout,JsonOptions).GetAwaiter().GetResult(); return Read<ConsignorSettlement>(response); }
        catch(HttpRequestException exception) { throw Unavailable(exception); } catch(TaskCanceledException exception) { throw Unavailable(exception); }
    }
    public Invoice RecordPayments(int bidderNumber,IReadOnlyList<PaymentRequest> payments)
    {
        LicenseSession.Current?.EnsureActive();
        try { using var response=_client.PostAsJsonAsync($"/api/invoices/{bidderNumber}/payments",payments,JsonOptions).GetAwaiter().GetResult(); return Read<Invoice>(response); }
        catch(HttpRequestException exception) { throw Unavailable(exception); } catch(TaskCanceledException exception) { throw Unavailable(exception); }
    }
    public Invoice RecordRefund(int bidderNumber,RefundRequest refund)=>Post<RefundRequest,Invoice>($"/api/invoices/{bidderNumber}/refunds",refund);
    public Invoice CorrectSale(int bidderNumber,SaleCorrectionRequest correction)=>Post<SaleCorrectionRequest,Invoice>($"/api/invoices/{bidderNumber}/sale-corrections",correction);
    public async Task<bool> CheckConnectionAsync(CancellationToken cancellationToken=default)
    {
        try { using var response=await _client.GetAsync("/api/health",cancellationToken); return response.IsSuccessStatusCode; }
        catch(HttpRequestException) { return false; } catch(TaskCanceledException) { return false; }
    }
    private T Get<T>(string path)
    {
        LicenseSession.Current?.EnsureActive();
        try { using var response=_client.GetAsync(path).GetAwaiter().GetResult(); return Read<T>(response); }
        catch(HttpRequestException exception) { throw Unavailable(exception); } catch(TaskCanceledException exception) { throw Unavailable(exception); }
    }
    private TResponse Post<TRequest,TResponse>(string path,TRequest request){LicenseSession.Current?.EnsureActive();try{using var response=_client.PostAsJsonAsync(path,request,JsonOptions).GetAwaiter().GetResult();return Read<TResponse>(response);}catch(HttpRequestException exception){throw Unavailable(exception);}catch(TaskCanceledException exception){throw Unavailable(exception);}}
    private static T Read<T>(HttpResponseMessage response)
    {
        if(response.IsSuccessStatusCode) return response.Content.ReadFromJsonAsync<T>(JsonOptions).GetAwaiter().GetResult() ?? throw new InvalidOperationException("The host returned an empty response.");
        var body=response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        try { using var json=JsonDocument.Parse(body); if(json.RootElement.TryGetProperty("detail",out var detail)) throw new InvalidOperationException(detail.GetString()); } catch(JsonException) { }
        throw new InvalidOperationException(response.StatusCode==System.Net.HttpStatusCode.Unauthorized ? "This cashier is not paired with the live-auction PC." : $"The auction host returned {response.StatusCode}.");
    }
    private static string Normalize(string value)=>new(value.Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());
    private static InvalidOperationException Unavailable(Exception exception)=>new("Cannot reach the live-auction PC. Check that HammerTrack is running there and both computers are on the same private network.",exception);
    public void Dispose()=>_client.Dispose();
}
