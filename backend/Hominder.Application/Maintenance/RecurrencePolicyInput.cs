using Hominder.Domain.Maintenance;

namespace Hominder.Application.Maintenance;

public enum RecurrenceKind
{
    Interval,
    MonthWindow,
    FixedDate,
    OneOff,
}

public sealed record RecurrencePolicyInput(
    RecurrenceKind Kind,
    int? IntervalAmount,
    RecurrenceUnit? IntervalUnit,
    DateOnly? StartReference,
    int? StartMonth,
    int? EndMonth,
    DateOnly? DueDate);
