using HammerTrackPro2.Forms;
using HammerTrackPro2.Services;
using HammerTrackPro2.ViewModels;
using System.Windows.Forms;

namespace HammerTrackPro2.Tests;

public sealed class WholesaleActivationFormTests
{
    [Fact]
    public void ActivationScreenProvidesSelectableMachineIdAndCopyButton()
    {
        using var form=new WholesaleActivationForm(new WholesaleActivationViewModel(new StubEntitlementService()),"Activation required.");
        var controls=form.Controls.Cast<Control>().SelectMany(Descendants).ToList();
        var machineId=Assert.Single(controls.OfType<TextBox>(),control=>control.Name=="WholesaleMachineId");
        var copy=Assert.Single(controls.OfType<Button>(),control=>control.Name=="CopyWholesaleMachineId");
        Assert.True(machineId.ReadOnly);Assert.False(machineId.TabStop);Assert.False(string.IsNullOrWhiteSpace(machineId.Text));Assert.Equal("Copy Machine ID",copy.Text);
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        foreach(Control child in control.Controls){yield return child;foreach(var descendant in Descendants(child))yield return descendant;}
    }

    private sealed class StubEntitlementService:IModuleEntitlementService
    {
        public bool IsActivated(string module,out string reason){reason=string.Empty;return false;}
        public ModuleActivationPayload Activate(string activationKey,string module)=>throw new NotSupportedException();
    }
}
