using HammerTrackLive.Web.Data;

namespace HammerTrackLive.Web.Services;

public static class OrganizationMembershipPolicy
{
    public static bool CanRemoveOrDemote(string currentRole, int administratorCount)
    {
        return currentRole != OrganizationRoles.Administrator || administratorCount > 1;
    }
}
