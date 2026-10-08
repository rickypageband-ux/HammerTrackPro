using System.Diagnostics;

namespace HammerTrackPro2.Services;

/// <summary>A validated license for this process. The entry point installs it before opening any operational screen.</summary>
public sealed class LicenseAccess
{
    private readonly Func<DateTime> _utcNow;
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private DateTime _startedUtc;
    public LicensePayload License { get; private set; }
    public bool IsDemo => License.Edition == "Demo";
    public bool IsActive
    {
        get
        {
            var now = EffectiveUtc;
            return (!IsDemo || now >= License.IssuedUtc.ToUniversalTime())
                && (!License.ExpiresUtc.HasValue || now < License.ExpiresUtc.Value.ToUniversalTime());
        }
    }
    private DateTime EffectiveUtc => new(Math.Max(_utcNow().Ticks, (_startedUtc + _elapsed.Elapsed).Ticks), DateTimeKind.Utc);
    public LicenseAccess(LicensePayload license, Func<DateTime>? utcNow = null)
    {
        _utcNow = utcNow ?? (() => DateTime.UtcNow);
        License = license;
        _startedUtc = _utcNow();
    }
    public void Replace(LicensePayload license)
    {
        License = license;
        _startedUtc = _utcNow();
        _elapsed.Restart();
    }
    public void EnsureActive()
    {
        if (!IsActive) throw new InvalidOperationException("This license has expired or the computer clock is incorrect. Import a valid license to continue. Your auction data is preserved.");
    }
    public void EnsureFullFeature(string feature)
    {
        EnsureActive();
        if (IsDemo) throw new InvalidOperationException($"{feature} is unavailable in the seven-day demo. Import a paid license to unlock it. Wholesale also requires its module activation.");
    }
}

public static class LicenseSession
{
    // Unset only in isolated test/design hosts. Program initializes this before setup, services or forms.
    public static LicenseAccess? Current { get; internal set; }
}
