using System.ComponentModel.DataAnnotations;
using System.Text;
using HammerTrackLive.Web.Data;
using HammerTrackLive.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace HammerTrackLive.Web.Pages.Account;

[EnableRateLimiting("authentication")]
public sealed class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IAccountEmailSender emailSender,
    ILogger<ForgotPasswordModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RequestSubmitted { get; private set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email.Trim());
        if (user is not null && user.IsActive && user.EmailConfirmed)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { email = user.Email, code = encodedToken },
                protocol: Request.Scheme);
            if (resetUrl is not null)
            {
                try
                {
                    await emailSender.SendPasswordResetAsync(user.Email!, user.DisplayName, resetUrl);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Password reset email delivery failed for user {UserId}.", user.Id);
                }
            }
        }

        RequestSubmitted = true;
        return Page();
    }

    public sealed class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
