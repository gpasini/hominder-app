using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class FixedDateAndOneOffPolicyTests
{
    [Fact]
    public void FixedDate_WindowIsTheStoredDate()
    {
        var policy = new FixedDatePolicy(new DateOnly(2027, 6, 30));

        var window = policy.NextDueWindow(new DateOnly(2026, 1, 1), []);

        Assert.Equal(new DateOnly(2027, 6, 30), window.OpenDate);
        Assert.Equal(new DateOnly(2027, 6, 30), window.DueDate);
        Assert.True(policy.RequiresNextDueOverride);
    }

    [Fact]
    public void FixedDate_WithCompletion_MovesToOverrideDate()
    {
        var policy = new FixedDatePolicy(new DateOnly(2025, 6, 30));

        var next = policy.WithCompletion(new DateOnly(2025, 6, 20), new DateOnly(2027, 6, 30));

        Assert.Equal(new DateOnly(2027, 6, 30), Assert.IsType<FixedDatePolicy>(next).DueDate);
    }

    [Fact]
    public void FixedDate_WithCompletion_WithoutOverride_Throws()
    {
        var policy = new FixedDatePolicy(new DateOnly(2025, 6, 30));

        Assert.Throws<DomainException>(() => policy.WithCompletion(new DateOnly(2025, 6, 20), null));
    }

    [Fact]
    public void OneOff_BecomesTerminalAfterCompletion()
    {
        var policy = new OneOffPolicy(new DateOnly(2026, 9, 1));

        Assert.False(policy.IsTerminal([]));
        Assert.True(policy.IsTerminal([new DateOnly(2026, 8, 20)]));
        Assert.False(policy.RequiresNextDueOverride);
    }
}
