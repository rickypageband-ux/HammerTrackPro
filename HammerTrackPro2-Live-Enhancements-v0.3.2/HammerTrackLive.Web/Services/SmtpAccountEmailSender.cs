using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace HammerTrackLive.Web.Services;

public sealed class SmtpAccountEmailSender(IOptions<AccountEmailOptions> options) : IAccountEmailSender
{
    private readonly AccountEmailOptions _options = options.Value;

    public Task SendInvitationAsync(string email, string displayName, string organizationName, string invitationUrl)
    {
        var subject = $"Your invitation to {organizationName} on HammerTrack Online";
        var body = $"""
            <p>Hello {HtmlEncoder.Default.Encode(displayName)},</p>
            <p>You have been invited to join <strong>{HtmlEncoder.Default.Encode(organizationName)}</strong> on HammerTrack Online.</p>
            <p><a href="{HtmlEncoder.Default.Encode(invitationUrl)}">Accept your invitation and create your password</a></p>
            <p>This private link expires in seven days and can be used only once. If you did not expect this invitation, you can ignore this email.</p>
            """;
        return SendAsync(email, subject, body);
    }

    public Task SendPasswordResetAsync(string email, string displayName, string resetUrl)
    {
        var body = $"""
            <p>Hello {HtmlEncoder.Default.Encode(displayName)},</p>
            <p>A password reset was requested for your HammerTrack Online account.</p>
            <p><a href="{HtmlEncoder.Default.Encode(resetUrl)}">Reset your password</a></p>
            <p>If you did not request this change, no action is required.</p>
            """;
        return SendAsync(email, "Reset your HammerTrack Online password", body);
    }

    private async Task SendAsync(string recipient, string subject, string body)
    {
        EnsureConfigured();
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password)
        };
        await client.SendMailAsync(message);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
            || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            throw new InvalidOperationException("Account email delivery is not configured.");
        }
    }
}
