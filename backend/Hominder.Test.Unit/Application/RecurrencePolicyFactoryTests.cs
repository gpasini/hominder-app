using Hominder.Application.Maintenance;
using Hominder.Domain.Common;
using Hominder.Domain.Maintenance;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Test.Unit.Application;

public class RecurrencePolicyFactoryTests
{
    [Fact]
    public void Create_Interval_BuildsIntervalPolicy()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.Interval, 2, RecurrenceUnit.Years, new DateOnly(2024, 5, 1), null, null, null);

        var policy = RecurrencePolicyFactory.Create(input);

        var interval = Assert.IsType<IntervalPolicy>(policy);
        Assert.Equal(2, interval.Amount);
        Assert.Equal(RecurrenceUnit.Years, interval.Unit);
    }

    [Fact]
    public void Create_MonthWindow_BuildsMonthWindowPolicy()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.MonthWindow, null, null, null, 3, 5, null);

        Assert.IsType<MonthWindowPolicy>(RecurrencePolicyFactory.Create(input));
    }

    [Fact]
    public void Create_FixedDate_BuildsFixedDatePolicy()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.FixedDate, null, null, null, null, null, new DateOnly(2027, 6, 30));

        Assert.IsType<FixedDatePolicy>(RecurrencePolicyFactory.Create(input));
    }

    [Fact]
    public void Create_Interval_WithMissingFields_Throws()
    {
        var input = new RecurrencePolicyInput(
            RecurrenceKind.Interval, null, null, null, null, null, null);

        Assert.Throws<DomainException>(() => RecurrencePolicyFactory.Create(input));
    }
}
