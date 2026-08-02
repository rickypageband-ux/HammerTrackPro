using HammerTrackPro2.Services;

namespace HammerTrackPro2.Tests;

public sealed class WorkstationConfigurationTests
{
    [Fact]
    public void Setup_IsRequiredUntilRoleSelectionIsExplicitlyCompleted()
    {
        var configuration=new WorkstationConfiguration { Role=WorkstationRole.Host };

        Assert.True(WorkstationConfigurationService.RequiresSetup(false,configuration));
        Assert.True(WorkstationConfigurationService.RequiresSetup(true,configuration));

        configuration.SetupCompleted=true;
        Assert.False(WorkstationConfigurationService.RequiresSetup(true,configuration));
    }
}
