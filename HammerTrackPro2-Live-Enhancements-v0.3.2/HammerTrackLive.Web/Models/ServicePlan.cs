namespace HammerTrackLive.Web.Models;

public sealed record ServicePlan(
    string Name,
    string Price,
    string Description,
    IReadOnlyList<string> Features,
    bool Featured = false);
