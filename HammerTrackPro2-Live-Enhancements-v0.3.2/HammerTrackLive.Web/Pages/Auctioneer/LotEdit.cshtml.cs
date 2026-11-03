using System.ComponentModel.DataAnnotations;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HammerTrackLive.Web.Pages.Auctioneer;

[Authorize]
[RequestSizeLimit(16 * 1024 * 1024)]
public sealed class LotEditModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IOrganizationAdministrationService authorization, ILotMediaStorage storage) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid OrganizationId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid AuctionId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid LotId { get; set; }
    [BindProperty] public LotInput Input { get; set; } = new();
    [BindProperty] public IFormFile? Upload { get; set; }
    [TempData] public string? StatusMessage { get; set; }
    public IReadOnlyList<MediaView> Media { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        if (LotId == Guid.Empty)
        {
            Input.LotNumber = (await dbContext.OnlineLots.Where(item => item.AuctionId == AuctionId).MaxAsync(item => (int?)item.LotNumber, HttpContext.RequestAborted) ?? 0) + 1;
            return Page();
        }
        return await LoadAsync() ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        if (!ModelState.IsValid) { if (LotId != Guid.Empty) await LoadMediaAsync(); return Page(); }
        var duplicate = await dbContext.OnlineLots.AnyAsync(item => item.AuctionId == AuctionId && item.LotNumber == Input.LotNumber && item.Id != LotId, HttpContext.RequestAborted);
        if (duplicate) { ModelState.AddModelError(nameof(Input.LotNumber), "That lot number is already used in this auction."); if (LotId != Guid.Empty) await LoadMediaAsync(); return Page(); }
        OnlineLot lot;
        if (LotId == Guid.Empty) { lot = new OnlineLot { Id = Guid.NewGuid(), AuctionId = AuctionId, CreatedUtc = DateTimeOffset.UtcNow }; dbContext.OnlineLots.Add(lot); }
        else { lot = await dbContext.OnlineLots.SingleOrDefaultAsync(item => item.Id == LotId && item.AuctionId == AuctionId, HttpContext.RequestAborted) ?? throw new InvalidOperationException("Lot not found."); }
        lot.LotNumber = Input.LotNumber; lot.Title = Input.Title.Trim(); lot.Description = Input.Description.Trim(); lot.Category = Input.Category.Trim();
        lot.Condition = Input.Condition.Trim(); lot.StartingBid = Input.StartingBid; lot.BidIncrement = Input.BidIncrement; lot.ReservePrice = Input.ReservePrice;
        lot.Quantity = Input.Quantity; lot.IsActive = true; lot.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
        StatusMessage = "Lot saved.";
        return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = lot.Id });
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        if (Upload is null || Upload.Length == 0) { StatusMessage = "Choose a JPG, PNG, WebP, or PDF file."; return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId }); }
        var lotExists = await dbContext.OnlineLots.AnyAsync(item => item.Id == LotId && item.AuctionId == AuctionId, HttpContext.RequestAborted);
        if (!lotExists) return NotFound();
        StoredMedia? stored = null;
        try
        {
            await using var stream = Upload.OpenReadStream();
            stored = await storage.SaveAsync(stream, Upload.ContentType, Upload.FileName, HttpContext.RequestAborted);
            var sortOrder = (await dbContext.OnlineLotMedia.Where(item => item.LotId == LotId).MaxAsync(item => (int?)item.SortOrder, HttpContext.RequestAborted) ?? -1) + 1;
            dbContext.OnlineLotMedia.Add(new OnlineLotMedia { Id = Guid.NewGuid(), LotId = LotId, StorageKey = stored.StorageKey, OriginalFileName = stored.OriginalFileName, ContentType = stored.ContentType, SizeBytes = stored.SizeBytes, Kind = stored.Kind, SortOrder = sortOrder, CreatedUtc = DateTimeOffset.UtcNow });
            await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
            StatusMessage = "File uploaded.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or DbUpdateException)
        {
            if (stored is not null) await storage.DeleteAsync(stored.StorageKey, HttpContext.RequestAborted);
            StatusMessage = exception.Message;
        }
        return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId });
    }

    public async Task<IActionResult> OnPostRemoveMediaAsync(Guid mediaId)
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        var media = await dbContext.OnlineLotMedia.Include(item => item.Lot).SingleOrDefaultAsync(item => item.Id == mediaId && item.LotId == LotId && item.Lot.AuctionId == AuctionId, HttpContext.RequestAborted);
        if (media is null) return NotFound();
        dbContext.OnlineLotMedia.Remove(media); await dbContext.SaveChangesAsync(HttpContext.RequestAborted); await storage.DeleteAsync(media.StorageKey, HttpContext.RequestAborted);
        StatusMessage = "File removed."; return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId });
    }

    private async Task<bool> LoadAsync()
    {
        var lot = await dbContext.OnlineLots.AsNoTracking().SingleOrDefaultAsync(item => item.Id == LotId && item.AuctionId == AuctionId, HttpContext.RequestAborted);
        if (lot is null) return false;
        Input = new LotInput { LotNumber = lot.LotNumber, Title = lot.Title, Description = lot.Description, Category = lot.Category, Condition = lot.Condition, StartingBid = lot.StartingBid, BidIncrement = lot.BidIncrement, ReservePrice = lot.ReservePrice, Quantity = lot.Quantity };
        await LoadMediaAsync(); return true;
    }
    private async Task LoadMediaAsync() => Media = await dbContext.OnlineLotMedia.AsNoTracking().Where(item => item.LotId == LotId).OrderBy(item => item.SortOrder).Select(item => new MediaView(item.Id, item.OriginalFileName, item.Kind)).ToListAsync(HttpContext.RequestAborted);
    private async Task<bool> CanEditAuctionAsync() { var user = await userManager.GetUserAsync(User); if (user is null || !await authorization.CanAdministerAsync(user.Id, OrganizationId, await userManager.IsInRoleAsync(user, PlatformRoles.Administrator), HttpContext.RequestAborted)) return false; return await dbContext.OnlineAuctions.AnyAsync(item => item.Id == AuctionId && item.OrganizationId == OrganizationId && item.Status == AuctionStatuses.Draft, HttpContext.RequestAborted); }

    public sealed class LotInput
    {
        [Range(1, int.MaxValue), Display(Name = "Lot number")] public int LotNumber { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [StringLength(5000)] public string Description { get; set; } = string.Empty;
        [StringLength(100)] public string Category { get; set; } = string.Empty;
        [StringLength(100)] public string Condition { get; set; } = string.Empty;
        [Range(0, 999999999), Display(Name = "Starting bid")] public decimal StartingBid { get; set; }
        [Range(0.01, 999999999), Display(Name = "Bid increment")] public decimal BidIncrement { get; set; } = 1;
        [Range(0, 999999999), Display(Name = "Reserve price")] public decimal? ReservePrice { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    }
    public sealed record MediaView(Guid Id, string OriginalFileName, string Kind);
}
