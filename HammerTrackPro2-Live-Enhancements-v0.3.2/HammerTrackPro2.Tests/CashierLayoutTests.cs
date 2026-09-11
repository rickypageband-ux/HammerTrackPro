using HammerTrackPro2.Forms;
using System.Windows.Forms;

namespace HammerTrackPro2.Tests;

public sealed class CashierLayoutTests
{
    [Fact]
    public void ActionRowsRemainHorizontallyScrollableWithoutWrappingOrClipping()
    {
        using var row = CashierForm.ActionFlow();

        Assert.True(row.AutoScroll);
        Assert.False(row.WrapContents);
        Assert.False(row.AutoSize);
        Assert.Equal(FlowDirection.LeftToRight, row.FlowDirection);
        Assert.True(row.MinimumSize.Height >= 68);
    }
}
