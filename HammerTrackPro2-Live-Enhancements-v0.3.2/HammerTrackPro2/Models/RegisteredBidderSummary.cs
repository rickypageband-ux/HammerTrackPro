namespace HammerTrackPro2.Models;

public sealed class RegisteredBidderSummary
{
    public int BuyerNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DriversLicense { get; set; } = string.Empty;
}
