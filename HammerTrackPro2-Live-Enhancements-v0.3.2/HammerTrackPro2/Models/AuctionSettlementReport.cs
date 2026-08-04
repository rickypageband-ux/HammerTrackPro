namespace HammerTrackPro2.Models;
public sealed class StaffPayment
{
    public long StaffPaymentId{get;set;} public long AuctionId{get;set;} public string Category{get;set;}=string.Empty; public string Payee{get;set;}=string.Empty;
    public decimal Amount{get;set;} public string Method{get;set;}=string.Empty; public DateTime PaidUtc{get;set;} public string Notes{get;set;}=string.Empty;
    public long? VoidsStaffPaymentId{get;set;} public long? ReplacementForStaffPaymentId{get;set;} public string VoidReason{get;set;}=string.Empty;
}
public sealed class AuctionSettlementReport
{
    public long AuctionId{get;set;} public string AuctionNumber{get;set;}=string.Empty; public string AuctionName{get;set;}=string.Empty;
    public decimal HammerSales{get;set;} public decimal BuyerPremiums{get;set;} public decimal CreditCardFees{get;set;} public decimal SalesTaxCollected{get;set;}
    public decimal InvoiceTotal{get;set;} public decimal InvoiceAmountPaid{get;set;} public decimal InvoiceBalanceUnpaid{get;set;}
    public decimal ConsignorGrossSales{get;set;} public decimal ConsignorCommissions{get;set;} public decimal PurchaseBalanceOffsets{get;set;} public decimal ConsignorPayouts{get;set;}
    public decimal StaffPayments{get;set;} public decimal NetCashMovement=>InvoiceAmountPaid-ConsignorPayouts-StaffPayments; public List<StaffPayment> StaffLedger{get;set;}=[];
}
