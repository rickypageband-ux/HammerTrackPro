namespace HammerTrackLive.Web.Services;

public static class LotCatalogChoices
{
    public static IReadOnlyList<string> Categories { get; } =
    [
        "Antiques & Collectibles",
        "Automotive",
        "Business & Industrial",
        "Clothing & Accessories",
        "Coins & Currency",
        "Electronics",
        "Farm & Equipment",
        "Furniture",
        "Household",
        "Jewelry",
        "Lawn & Garden",
        "Sports & Outdoors",
        "Tools",
        "Toys & Games",
        "Other"
    ];

    public static IReadOnlyList<string> Conditions { get; } =
    [
        "New",
        "Like New",
        "Excellent",
        "Good",
        "Fair",
        "Used",
        "For Parts or Repair",
        "Unknown"
    ];

    public const int MaximumFilesPerUpload = 5;
    public const int MaximumFilesPerLot = 20;

    public static int GetNextLotNumber(IEnumerable<int> existingLotNumbers)
    {
        var highest = existingLotNumbers.Where(number => number > 0).DefaultIfEmpty(0).Max();
        return checked(highest + 1);
    }
}
