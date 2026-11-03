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
[RequestSizeLimit(64 * 1024 * 1024)]
public sealed class LotEditModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IOrganizationAdministrationService authorization, ILotMediaStorage storage) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid OrganizationId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid AuctionId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid LotId { get; set; }
    [BindProperty] public LotInput Input { get; set; } = new();
    [BindProperty] public List<IFormFile> Uploads { get; set; } = [];
    [TempData] public string? StatusMessage { get; set; }
    public IReadOnlyList<MediaView> Media { get; private set; } = [];
    public IReadOnlyList<string> Categories => LotCatalogChoices.Categories;
    public IReadOnlyList<string> Conditions => LotCatalogChoices.Conditions;
    public int MaximumFilesPerLot => LotCatalogChoices.MaximumFilesPerLot;
    public string AuctionTitle { get; private set; } = string.Empty;
    public string AuctionStatus { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        if (LotId == Guid.Empty)
        {
            var existingLotNumbers = await dbContext.OnlineLots.Where(item => item.AuctionId == AuctionId && item.IsActive).Select(item => item.LotNumber).ToListAsync(HttpContext.RequestAborted);
            Input.LotNumber = LotCatalogChoices.GetNextLotNumber(existingLotNumbers);
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
        lot.LotNumber = Input.LotNumber; lot.Title = Input.Title.Trim(); lot.Description = Input.Description?.Trim() ?? string.Empty; lot.Category = Input.Category?.Trim() ?? string.Empty;
        lot.Condition = Input.Condition?.Trim() ?? string.Empty; lot.StartingBid = Input.StartingBid; lot.BidIncrement = Input.BidIncrement; lot.ReservePrice = Input.ReservePrice;
        lot.Quantity = Input.Quantity; lot.IsActive = true; lot.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
        StatusMessage = "Lot saved.";
        return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = lot.Id });
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        if (Uploads.Count == 0 || Uploads.All(file => file.Length == 0)) { StatusMessage = "Choose one or more JPG, PNG, WebP, or PDF files."; return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId }); }
        if (Uploads.Count > LotCatalogChoices.MaximumFilesPerUpload) { StatusMessage = $"Upload no more than {LotCatalogChoices.MaximumFilesPerUpload} files at one time."; return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId }); }
        var lotExists = await dbContext.OnlineLots.AnyAsync(item => item.Id == LotId && item.AuctionId == AuctionId, HttpContext.RequestAborted);
        if (!lotExists) return NotFound();
        var existingCount = await dbContext.OnlineLotMedia.CountAsync(item => item.LotId == LotId, HttpContext.RequestAborted);
        var files = Uploads.Where(file => file.Length > 0).ToList();
        if (existingCount + files.Count > LotCatalogChoices.MaximumFilesPerLot) { StatusMessage = $"Each lot can have up to {LotCatalogChoices.MaximumFilesPerLot} photos and documents."; return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId }); }
        var storedFiles = new List<StoredMedia>();
        try
        {
            var sortOrder = (await dbContext.OnlineLotMedia.Where(item => item.LotId == LotId).MaxAsync(item => (int?)item.SortOrder, HttpContext.RequestAborted) ?? -1) + 1;
            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();
                var stored = await storage.SaveAsync(stream, file.ContentType, file.FileName, HttpContext.RequestAborted);
                storedFiles.Add(stored);
                dbContext.OnlineLotMedia.Add(new OnlineLotMedia { Id = Guid.NewGuid(), LotId = LotId, StorageKey = stored.StorageKey, OriginalFileName = stored.OriginalFileName, ContentType = stored.ContentType, SizeBytes = stored.SizeBytes, Kind = stored.Kind, SortOrder = sortOrder++, CreatedUtc = DateTimeOffset.UtcNow });
            }
            await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
            StatusMessage = files.Count == 1 ? "File added to this lot." : $"{files.Count} files added to this lot.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or DbUpdateException)
        {
            foreach (var stored in storedFiles) await storage.DeleteAsync(stored.StorageKey, HttpContext.RequestAborted);
            StatusMessage = exception.Message;
        }
        return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId, lotId = LotId });
    }

    public async Task<IActionResult> OnPostMakePrimaryAsync(Guid mediaId)
    {
        if (!await CanEditAuctionAsync()) return Forbid();
        var media = await dbContext.OnlineLotMedia.Where(item => item.LotId == LotId).OrderBy(item => item.SortOrder).ToListAsync(HttpContext.RequestAborted);
        var selected = media.SingleOrDefault(item => item.Id == mediaId && item.Kind == "Image");
        if (selected is null) return NotFound();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(HttpContext.RequestAborted);
        foreach (var item in media) item.SortOrder += 1000;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
        var ordered = media.Where(item => item.Id != selected.Id).Prepend(selected).ToList();
        for (var index = 0; index < ordered.Count; index++) ordered[index].SortOrder = index;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
        await transaction.CommitAsync(HttpContext.RequestAborted);
        StatusMessage = "Cover photo updated for this lot.";
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
    private async Task LoadMediaAsync() => Media = await dbContext.OnlineLotMedia.AsNoTracking().Where(item => item.LotId == LotId).OrderBy(item => item.SortOrder).Select(item => new MediaView(item.Id, item.OriginalFileName, item.Kind, item.SortOrder == 0 && item.Kind == "Image")).ToListAsync(HttpContext.RequestAborted);
    private async Task<bool> CanEditAuctionAsync() { var user = await userManager.GetUserAsync(User); if (user is null || !await authorization.CanAdministerAsync(user.Id, OrganizationId, await userManager.IsInRoleAsync(user, PlatformRoles.Administrator), HttpContext.RequestAborted)) return false; var auction = await dbContext.OnlineAuctions.AsNoTracking().Where(item => item.Id == AuctionId && item.OrganizationId == OrganizationId).Select(item => new { item.Title, item.Status }).SingleOrDefaultAsync(HttpContext.RequestAborted); if (auction is null) return false; AuctionTitle = auction.Title; AuctionStatus = auction.Status; return AuctionCatalogEditingPolicy.CanManageLots(auction.Status); }

    public sealed class LotInput
    {
        [Range(1, int.MaxValue), Display(Name = "Lot number")] public int LotNumber { get; set; }
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [StringLength(5000)] public string? Description { get; set; }
        [StringLength(100)] public string? Category { get; set; }
        [StringLength(100)] public string? Condition { get; set; }
        [Range(0, 999999999), Display(Name = "Starting bid")] public decimal StartingBid { get; set; }
        [Range(0.01, 999999999), Display(Name = "Bid increment")] public decimal BidIncrement { get; set; } = 1;
        [Range(0, 999999999), Display(Name = "Reserve price")] public decimal? ReservePrice { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    }
    public sealed record MediaView(Guid Id, string OriginalFileName, string Kind, bool IsPrimary);
}
