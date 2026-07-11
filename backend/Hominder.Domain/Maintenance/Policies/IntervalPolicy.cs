using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public sealed class IntervalPolicy : RecurrencePolicy
{
    public IntervalPolicy(int amount, RecurrenceUnit unit, DateOnly startReference)
    {
        if (amount <= 0)
        {
            throw new DomainException("L'intervalle doit être strictement positif.");
        }

        Amount = amount;
        Unit = unit;
        StartReference = startReference;
    }

    public int Amount { get; }

    public RecurrenceUnit Unit { get; }

    public DateOnly StartReference { get; }

    public override bool RequiresNextDueOverride => false;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions)
    {
        var reference = completions.Count > 0 ? completions.Max() : StartReference;
        var due = AddInterval(reference);
        return new DueWindow(due, due);
    }

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => false;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride) => this;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Unit;
        yield return StartReference;
    }

    private DateOnly AddInterval(DateOnly reference) => Unit switch
    {
        RecurrenceUnit.Days => reference.AddDays(Amount),
        RecurrenceUnit.Weeks => reference.AddDays(7 * Amount),
        RecurrenceUnit.Months => reference.AddMonths(Amount),
        RecurrenceUnit.Years => reference.AddYears(Amount),
        _ => throw new DomainException("Unité de récurrence inconnue."),
    };
}
