using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public sealed class MonthWindowPolicy : RecurrencePolicy
{
    public MonthWindowPolicy(int startMonth, int endMonth)
    {
        if (startMonth is < 1 or > 12)
        {
            throw new DomainException("Le mois de début doit être compris entre 1 et 12.");
        }

        if (endMonth is < 1 or > 12)
        {
            throw new DomainException("Le mois de fin doit être compris entre 1 et 12.");
        }

        StartMonth = startMonth;
        EndMonth = endMonth;
    }

    public int StartMonth { get; }

    public int EndMonth { get; }

    public override bool RequiresNextDueOverride => false;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions)
    {
        var cycleStartYear = EndMonth >= StartMonth
            ? today.Year
            : (today.Month >= StartMonth ? today.Year : today.Year - 1);
        var current = WindowForCycle(cycleStartYear);

        var lastCompletion = completions.Count > 0 ? completions.Max() : (DateOnly?)null;
        var satisfied = lastCompletion is DateOnly completed && completed >= current.OpenDate;

        return satisfied ? WindowForCycle(cycleStartYear + 1) : current;
    }

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => false;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride) => this;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartMonth;
        yield return EndMonth;
    }

    private DueWindow WindowForCycle(int startYear)
    {
        var open = new DateOnly(startYear, StartMonth, 1);
        var endYear = EndMonth >= StartMonth ? startYear : startYear + 1;
        var due = new DateOnly(endYear, EndMonth, DateTime.DaysInMonth(endYear, EndMonth));
        return new DueWindow(open, due);
    }
}
