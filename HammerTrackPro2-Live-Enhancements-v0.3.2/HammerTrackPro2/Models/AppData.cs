namespace HammerTrackPro2.Models;

public sealed class AppData
{
    public List<Bidder> Bidders { get; set; } = new();
    public List<Lot> Lots { get; set; } = new();
}
