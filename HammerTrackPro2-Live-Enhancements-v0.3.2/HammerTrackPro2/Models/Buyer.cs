namespace HammerTrackPro2.Models;

public sealed class Buyer
{
    public long BuyerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DriversLicense { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ResaleLicenseNumber { get; set; } = string.Empty;
    public string ResaleLicenseState { get; set; } = string.Empty;
    public bool TaxExempt { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
}
