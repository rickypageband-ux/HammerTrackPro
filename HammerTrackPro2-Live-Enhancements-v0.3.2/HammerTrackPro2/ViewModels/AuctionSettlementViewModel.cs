using HammerTrackPro2.Models;
using HammerTrackPro2.Services;

namespace HammerTrackPro2.ViewModels;

public sealed class AuctionSettlementViewModel
{
    private readonly ICashierService _service;
    public AuctionSettlementViewModel(ICashierService service)=>_service=service??throw new ArgumentNullException(nameof(service));
    public AuctionSettlementReport Report{get;private set;}=new();
    public void Refresh()=>Report=_service.GetAuctionSettlementReport();
    public void RecordStaffPayment(string category,string payee,decimal amount,string method,string notes)
    {
        _service.RecordStaffPayment(new StaffPayment{Category=category,Payee=payee.Trim(),Amount=amount,Method=method.Trim(),Notes=notes.Trim()});
        Refresh();
    }
    public void VoidStaffPayment(StaffPayment payment,string reason)
    {
        ArgumentNullException.ThrowIfNull(payment);
        _service.VoidStaffPayment(payment.StaffPaymentId,reason.Trim(),null);
        Refresh();
    }
}
