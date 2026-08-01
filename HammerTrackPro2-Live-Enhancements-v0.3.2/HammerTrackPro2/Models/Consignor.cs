using System.ComponentModel;
namespace HammerTrackPro2.Models;

public sealed class Consignor
{
    public long ConsignorId { get; set; }
    public int ConsignorNumber { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public decimal DefaultCommissionPercent { get; set; }
    public decimal CommissionOverridePercent { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public bool PrintCheck { get; set; } = true;
    public bool HoldCheck { get; set; }
    public bool TaxExempt { get; set; }
    [DisplayName("1099 Required")]
    public bool Require1099 { get; set; }
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
}
