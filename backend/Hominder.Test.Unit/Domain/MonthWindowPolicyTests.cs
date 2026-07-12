using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class MonthWindowPolicyTests
{
    private static readonly MonthWindowPolicy Spring = new(3, 5);

    [Fact]
    public void CurrentCycleWindow_SpansStartToEndOfMonths()
    {
        var window = Spring.NextDueWindow(new DateOnly(2026, 4, 15), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2026, 5, 31), window.DueDate);
    }

    [Fact]
    public void BeforeWindowOpens_ReturnsThisYearWindow()
    {
        var window = Spring.NextDueWindow(new DateOnly(2026, 1, 10), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.OpenDate);
    }

    [Fact]
    public void CompletedInsideWindow_MovesToNextYear()
    {
        var window = Spring.NextDueWindow(
            new DateOnly(2026, 4, 20),
            [new DateOnly(2026, 4, 18)]);

        Assert.Equal(new DateOnly(2027, 3, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2027, 5, 31), window.DueDate);
    }

    [Fact]
    public void PastWindowNotCompleted_StaysOnCurrentCycle()
    {
        var window = Spring.NextDueWindow(new DateOnly(2026, 8, 1), []);

        Assert.Equal(new DateOnly(2026, 3, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2026, 5, 31), window.DueDate);
    }

    [Fact]
    public void WrapAroundMonths_HandlesYearBoundary()
    {
        var winter = new MonthWindowPolicy(11, 2);

        var window = winter.NextDueWindow(new DateOnly(2026, 1, 15), []);

        Assert.Equal(new DateOnly(2025, 11, 1), window.OpenDate);
        Assert.Equal(new DateOnly(2026, 2, 28), window.DueDate);
    }

    [Fact]
    public void Construct_WithInvalidMonth_Throws()
    {
        Assert.Throws<DomainException>(() => new MonthWindowPolicy(0, 5));
        Assert.Throws<DomainException>(() => new MonthWindowPolicy(3, 13));
    }
}
