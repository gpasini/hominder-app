using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Domain;

public class MaintenanceTaskTests
{
    private static MaintenanceTask NewSpringTask() =>
        MaintenanceTask.Create("Tailler l'olivier", null, new MonthWindowPolicy(3, 5), null);

    [Fact]
    public void Create_WithBlankTitle_Throws()
    {
        Assert.Throws<DomainException>(() =>
            MaintenanceTask.Create("  ", null, new OneOffPolicy(new DateOnly(2026, 9, 1)), null));
    }

    [Fact]
    public void Evaluate_InsideWindow_IsDue()
    {
        var task = NewSpringTask();

        var evaluation = task.Evaluate(new DateOnly(2026, 4, 15));

        Assert.Equal(MaintenanceStatus.Due, evaluation.Status);
        Assert.Equal(0, evaluation.DaysOverdue);
    }

    [Fact]
    public void Evaluate_BeforeWindow_IsUpcoming()
    {
        var task = MaintenanceTask.Create("CT", null, new FixedDatePolicy(new DateOnly(2027, 6, 30)), null);

        Assert.Equal(MaintenanceStatus.Upcoming, task.Evaluate(new DateOnly(2026, 1, 1)).Status);
    }

    [Fact]
    public void Evaluate_AfterDue_IsOverdueWithDayCount()
    {
        var task = MaintenanceTask.Create("CT", null, new FixedDatePolicy(new DateOnly(2026, 6, 30)), null);

        var evaluation = task.Evaluate(new DateOnly(2026, 7, 10));

        Assert.Equal(MaintenanceStatus.Overdue, evaluation.Status);
        Assert.Equal(10, evaluation.DaysOverdue);
    }

    [Fact]
    public void MarkDone_AddsCompletionAndRaisesEvent()
    {
        var task = NewSpringTask();
        var member = HouseholdMemberId.New();

        task.MarkDone(new DateOnly(2026, 4, 18), member, null);

        Assert.Single(task.Completions);
        Assert.Contains(task.DomainEvents, e => e is MaintenanceTaskCompletedDomainEvent);
    }

    [Fact]
    public void MarkDone_FixedDateWithoutOverride_Throws()
    {
        var task = MaintenanceTask.Create("CT", null, new FixedDatePolicy(new DateOnly(2026, 6, 30)), null);

        Assert.Throws<DomainException>(() =>
            task.MarkDone(new DateOnly(2026, 6, 20), HouseholdMemberId.New(), null));
    }

    [Fact]
    public void MarkDone_IntervalWithOverride_Throws()
    {
        var task = MaintenanceTask.Create(
            "Saturateur", null, new IntervalPolicy(2, RecurrenceUnit.Years, new DateOnly(2024, 5, 1)), null);

        Assert.Throws<DomainException>(() =>
            task.MarkDone(new DateOnly(2026, 5, 1), HouseholdMemberId.New(), new DateOnly(2028, 5, 1)));
    }

    [Fact]
    public void MarkDone_OneOffTwice_Throws()
    {
        var task = MaintenanceTask.Create("Poser étagère", null, new OneOffPolicy(new DateOnly(2026, 9, 1)), null);
        var member = HouseholdMemberId.New();
        task.MarkDone(new DateOnly(2026, 8, 20), member, null);

        Assert.Throws<DomainException>(() => task.MarkDone(new DateOnly(2026, 8, 21), member, null));
    }

    [Fact]
    public void Evaluate_OneOffCompleted_IsDone()
    {
        var task = MaintenanceTask.Create("Poser étagère", null, new OneOffPolicy(new DateOnly(2026, 9, 1)), null);
        task.MarkDone(new DateOnly(2026, 8, 20), HouseholdMemberId.New(), null);

        Assert.Equal(MaintenanceStatus.Done, task.Evaluate(new DateOnly(2026, 9, 5)).Status);
    }
}
