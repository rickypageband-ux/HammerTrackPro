namespace HammerTrackPro2.Models;

public sealed class DashboardSnapshot
{
    public int BidderCount { get; set; }
    public int LotCount { get; set; }
    public int SoldCount { get; set; }
    public decimal GrossSales { get; set; }
    public decimal AverageSalePrice { get; set; }
    public List<string> RecentActivity { get; set; } = new();
}
