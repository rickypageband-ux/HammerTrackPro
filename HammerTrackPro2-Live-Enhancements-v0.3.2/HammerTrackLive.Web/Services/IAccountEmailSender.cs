namespace HammerTrackLive.Web.Services;

public interface IAccountEmailSender
{
    Task SendInvitationAsync(string email, string displayName, string organizationName, string invitationUrl);

    Task SendPasswordResetAsync(string email, string displayName, string resetUrl);
}
