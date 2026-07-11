using Hominder.Domain.Common;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Application.Maintenance;

public static class RecurrencePolicyMapper
{
    public static RecurrencePolicyInput ToInput(RecurrencePolicy policy) => policy switch
    {
        IntervalPolicy i => new RecurrencePolicyInput(
            RecurrenceKind.Interval, i.Amount, i.Unit, i.StartReference, null, null, null),
        MonthWindowPolicy m => new RecurrencePolicyInput(
            RecurrenceKind.MonthWindow, null, null, null, m.StartMonth, m.EndMonth, null),
        FixedDatePolicy f => new RecurrencePolicyInput(
            RecurrenceKind.FixedDate, null, null, null, null, null, f.DueDate),
        OneOffPolicy o => new RecurrencePolicyInput(
            RecurrenceKind.OneOff, null, null, null, null, null, o.DueDate),
        _ => throw new DomainException("Politique de récurrence inconnue."),
    };
}
