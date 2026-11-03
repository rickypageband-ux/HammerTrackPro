using System.ComponentModel.DataAnnotations;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace HammerTrackLive.Web.Pages.Account;

[EnableRateLimiting("authentication")]
public sealed class AcceptInvitationModel(
    IAccountInvitationService invitationService,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public InvitationDetails? Invitation { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? token)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Account/Index");
        }

        Input.Token = token ?? string.Empty;
        Invitation = await invitationService.FindValidAsync(Input.Token, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Invitation = await invitationService.FindValidAsync(Input.Token, HttpContext.RequestAborted);
        if (Invitation is null)
        {
            ModelState.AddModelError(string.Empty, "This invitation is invalid or has expired.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await invitationService.AcceptAsync(Input.Token, Input.Password, HttpContext.RequestAborted);
        if (!result.Succeeded || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "The invitation could not be accepted.");
            return Page();
        }

        await signInManager.SignInAsync(result.User, isPersistent: false);
        return RedirectToPage("/Account/Index");
    }

    public sealed class InputModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(12)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
