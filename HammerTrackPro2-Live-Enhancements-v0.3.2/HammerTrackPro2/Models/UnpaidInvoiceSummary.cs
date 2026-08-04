namespace HammerTrackPro2.Models;
public sealed class UnpaidInvoiceSummary{public int BuyerNumber{get;set;}public string BuyerName{get;set;}=string.Empty;public decimal InvoiceTotal{get;set;}public decimal AmountPaid{get;set;}public decimal BalanceDue{get;set;}}
