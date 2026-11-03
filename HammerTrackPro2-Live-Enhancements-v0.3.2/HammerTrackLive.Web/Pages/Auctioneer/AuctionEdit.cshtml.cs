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
public sealed class AuctionEditModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IOrganizationAdministrationService authorization, IAuctionPublishingService publisher) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid OrganizationId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid AuctionId { get; set; }
    [BindProperty] public AuctionInput Input { get; set; } = new();
    [TempData] public string? StatusMessage { get; set; }
    public bool IsDraft { get; private set; } = true;
    public IReadOnlyList<LotView> Lots { get; private set; } = [];
    public IReadOnlyList<(string Id, string Name)> TimeZones => AuctionTimeZones.Supported;
    public IReadOnlyList<string> PublicationErrors { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await CanAdministerAsync()) return Forbid();
        if (AuctionId == Guid.Empty)
        {
            var now = DateTime.Now.AddDays(1);
            Input.StartsLocal = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
            Input.EndsLocal = Input.StartsLocal.AddDays(7);
            return Page();
        }
        return await LoadAsync() ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!await CanAdministerAsync()) return Forbid();
        if (!ModelState.IsValid) { if (AuctionId != Guid.Empty) await LoadLotsAsync(); return Page(); }
        string slug;
        DateTimeOffset startsUtc;
        DateTimeOffset endsUtc;
        try { slug = OrganizationSlug.Normalize(Input.Slug); startsUtc = AuctionTimeZones.ToUtc(Input.StartsLocal, Input.TimeZoneId); endsUtc = AuctionTimeZones.ToUtc(Input.EndsLocal, Input.TimeZoneId); }
        catch (ArgumentException exception) { ModelState.AddModelError(string.Empty, exception.Message); if (AuctionId != Guid.Empty) await LoadLotsAsync(); return Page(); }
        if (endsUtc <= startsUtc) { ModelState.AddModelError(nameof(Input.EndsLocal), "Closing time must be after opening time."); if (AuctionId != Guid.Empty) await LoadLotsAsync(); return Page(); }
        var duplicateSlug = await dbContext.OnlineAuctions.AnyAsync(item => item.Slug == slug && item.Id != AuctionId, HttpContext.RequestAborted);
        if (duplicateSlug) { ModelState.AddModelError(nameof(Input.Slug), "That public auction address is already in use."); if (AuctionId != Guid.Empty) await LoadLotsAsync(); return Page(); }

        OnlineAuction auction;
        if (AuctionId == Guid.Empty)
        {
            auction = new OnlineAuction { Id = Guid.NewGuid(), OrganizationId = OrganizationId, CreatedUtc = DateTimeOffset.UtcNow };
            dbContext.OnlineAuctions.Add(auction);
        }
        else
        {
            auction = await dbContext.OnlineAuctions.SingleOrDefaultAsync(item => item.Id == AuctionId && item.OrganizationId == OrganizationId, HttpContext.RequestAborted) ?? throw new InvalidOperationException("Auction not found.");
            if (auction.Status != AuctionStatuses.Draft) return BadRequest("Published auctions cannot be edited.");
        }
        auction.Slug = slug; auction.Title = Input.Title.Trim(); auction.Description = Input.Description.Trim(); auction.Location = Input.Location.Trim();
        auction.TimeZoneId = Input.TimeZoneId; auction.StartsUtc = startsUtc; auction.EndsUtc = endsUtc; auction.BuyerPremiumPercent = Input.BuyerPremiumPercent;
        auction.SalesTaxPercent = Input.SalesTaxPercent; auction.TaxBasis = Input.TaxBasis; auction.PaymentInstructions = Input.PaymentInstructions.Trim();
        auction.PickupInstructions = Input.PickupInstructions.Trim(); auction.ShippingAvailable = Input.ShippingAvailable; auction.ShippingInstructions = Input.ShippingInstructions.Trim();
        auction.UpdatedUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted);
        StatusMessage = "Auction draft saved.";
        return RedirectToPage(new { organizationId = OrganizationId, auctionId = auction.Id });
    }

    public async Task<IActionResult> OnPostPublishAsync()
    {
        if (!await CanAdministerAsync()) return Forbid();
        var result = await publisher.PublishAsync(OrganizationId, AuctionId, HttpContext.RequestAborted);
        if (!result.Succeeded) { PublicationErrors = result.Errors; await LoadAsync(); return Page(); }
        StatusMessage = "Auction published to the public catalog.";
        return RedirectToPage(new { organizationId = OrganizationId, auctionId = AuctionId });
    }

    private async Task<bool> LoadAsync()
    {
        var auction = await dbContext.OnlineAuctions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == AuctionId && item.OrganizationId == OrganizationId, HttpContext.RequestAborted);
        if (auction is null) return false;
        IsDraft = auction.Status == AuctionStatuses.Draft;
        Input = new AuctionInput { Title = auction.Title, Slug = auction.Slug, Description = auction.Description, Location = auction.Location, TimeZoneId = auction.TimeZoneId,
            StartsLocal = AuctionTimeZones.ToLocal(auction.StartsUtc, auction.TimeZoneId), EndsLocal = AuctionTimeZones.ToLocal(auction.EndsUtc, auction.TimeZoneId),
            BuyerPremiumPercent = auction.BuyerPremiumPercent, SalesTaxPercent = auction.SalesTaxPercent, TaxBasis = auction.TaxBasis,
            PaymentInstructions = auction.PaymentInstructions, PickupInstructions = auction.PickupInstructions, ShippingAvailable = auction.ShippingAvailable, ShippingInstructions = auction.ShippingInstructions };
        await LoadLotsAsync(); return true;
    }
    private async Task LoadLotsAsync() => Lots = await dbContext.OnlineLots.AsNoTracking().Where(item => item.AuctionId == AuctionId && item.IsActive).OrderBy(item => item.LotNumber).Select(item => new LotView(item.Id, item.LotNumber, item.Title, item.Media.Count)).ToListAsync(HttpContext.RequestAborted);
    private async Task<bool> CanAdministerAsync() { var user = await userManager.GetUserAsync(User); return user is not null && await authorization.CanAdministerAsync(user.Id, OrganizationId, await userManager.IsInRoleAsync(user, PlatformRoles.Administrator), HttpContext.RequestAborted); }

    public sealed class AuctionInput
    {
        [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
        [Required, StringLength(120), Display(Name = "Public address")] public string Slug { get; set; } = string.Empty;
        [Required, StringLength(5000)] public string Description { get; set; } = string.Empty;
        [Required, StringLength(300)] public string Location { get; set; } = string.Empty;
        [Required, Display(Name = "Opens")] public DateTime StartsLocal { get; set; }
        [Required, Display(Name = "Closes")] public DateTime EndsLocal { get; set; }
        [Required, Display(Name = "Time zone")] public string TimeZoneId { get; set; } = "America/New_York";
        [Range(0, 100), Display(Name = "Buyer premium %")] public decimal BuyerPremiumPercent { get; set; }
        [Range(0, 100), Display(Name = "Sales tax %")] public decimal SalesTaxPercent { get; set; }
        [Required, Display(Name = "Tax basis")] public string TaxBasis { get; set; } = "HammerPlusBuyerPremium";
        [Required, StringLength(3000), Display(Name = "Payment instructions")] public string PaymentInstructions { get; set; } = string.Empty;
        [Required, StringLength(3000), Display(Name = "Pickup instructions")] public string PickupInstructions { get; set; } = string.Empty;
        [Display(Name = "Shipping available")] public bool ShippingAvailable { get; set; }
        [StringLength(3000), Display(Name = "Shipping instructions")] public string ShippingInstructions { get; set; } = string.Empty;
    }
    public sealed record LotView(Guid Id, int LotNumber, string Title, int MediaCount);
}
