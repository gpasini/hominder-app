using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class IntervalPolicyTests
{
    private static readonly DateOnly Start = new(2024, 3, 1);

    [Fact]
    public void NextDueWindow_BeforeAnyCompletion_UsesStartReferencePlusInterval()
    {
        var policy = new IntervalPolicy(2, RecurrenceUnit.Years, Start);

        var window = policy.NextDueWindow(new DateOnly(2025, 1, 1), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.DueDate);
        Assert.Equal(window.OpenDate, window.DueDate);
    }

    [Fact]
    public void NextDueWindow_UsesLatestCompletionPlusInterval()
    {
        var policy = new IntervalPolicy(6, RecurrenceUnit.Months, Start);

        var window = policy.NextDueWindow(
            new DateOnly(2026, 1, 1),
            [new DateOnly(2025, 4, 10), new DateOnly(2025, 10, 5)]);

        Assert.Equal(new DateOnly(2026, 4, 5), window.DueDate);
    }

    [Fact]
    public void RequiresNextDueOverride_IsFalse_AndIsNotTerminal()
    {
        var policy = new IntervalPolicy(1, RecurrenceUnit.Weeks, Start);

        Assert.False(policy.RequiresNextDueOverride);
        Assert.False(policy.IsTerminal([new DateOnly(2025, 1, 1)]));
    }

    [Fact]
    public void Construct_WithNonPositiveAmount_Throws()
    {
        Assert.Throws<DomainException>(() => new IntervalPolicy(0, RecurrenceUnit.Days, Start));
    }
}
