using System.Security.Cryptography;

namespace HammerTrackLive.Web.Services;

public static class BootstrapPassword
{
    public static string Create()
    {
        return $"Aa1!{Convert.ToHexString(RandomNumberGenerator.GetBytes(24))}";
    }
}
