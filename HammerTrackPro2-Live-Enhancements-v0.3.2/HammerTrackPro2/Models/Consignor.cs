using System.ComponentModel;
namespace HammerTrackPro2.Models;

public sealed class Consignor
{
    [ReadOnly(true), Category("Identity")]
    public long ConsignorId { get; set; }
    [ReadOnly(true), Category("Identity"), DisplayName("Linked Buyer ID")]
    public long? LinkedBuyerId { get; set; }
    [Category("Identity"), DisplayName("Seller Number")]
    public int ConsignorNumber { get; set; }
    [Category("Identity"), DisplayName("Business Name")]
    public string BusinessName { get; set; } = string.Empty;
    [Category("Contact"), DisplayName("First Name")]
    public string FirstName { get; set; } = string.Empty;
    [Category("Contact"), DisplayName("Last Name")]
    public string LastName { get; set; } = string.Empty;
    [Category("Contact")]
    public string Address { get; set; } = string.Empty;
    [Category("Contact")]
    public string City { get; set; } = string.Empty;
    [Category("Contact")]
    public string State { get; set; } = string.Empty;
    [Category("Contact"), DisplayName("ZIP")]
    public string Zip { get; set; } = string.Empty;
    [Category("Contact")]
    public string Phone { get; set; } = string.Empty;
    [Category("Contact")]
    public string Email { get; set; } = string.Empty;
    [Category("Tax and Settlement"), DisplayName("Tax ID")]
    public string TaxId { get; set; } = string.Empty;
    [Category("Commission"), DisplayName("Default Commission %")]
    public decimal DefaultCommissionPercent { get; set; }
    [Category("Commission"), DisplayName("Commission Override %")]
    public decimal CommissionOverridePercent { get; set; }
    [Category("Notes")]
    public string Notes { get; set; } = string.Empty;
    [Category("Identity")]
    public bool Active { get; set; } = true;
    [Category("Tax and Settlement"), DisplayName("Print Check")]
    public bool PrintCheck { get; set; } = true;
    [Category("Tax and Settlement"), DisplayName("Hold Check")]
    public bool HoldCheck { get; set; }
    [Category("Tax and Settlement"), DisplayName("Tax Exempt")]
    public bool TaxExempt { get; set; }
    [Category("Tax and Settlement"), DisplayName("1099 Required")]
    public bool Require1099 { get; set; }
    [ReadOnly(true), Category("Audit"), DisplayName("Created Date")]
    public DateTime CreatedDate { get; set; }

    [Browsable(false)]
    [Obsolete("Use Require1099. This compatibility alias will be removed after legacy callers migrate.")]
    public bool Is1099Required
    {
        get => Require1099;
        set => Require1099 = value;
    }
    public string ContactName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => string.IsNullOrWhiteSpace(BusinessName) ? ContactName : BusinessName;
    [Browsable(false)]
    public string LotDisplayName => $"#{ConsignorNumber} — {DisplayName}{(Active ? string.Empty : " (Inactive)")}";

    public override string ToString() => LotDisplayName;
}
