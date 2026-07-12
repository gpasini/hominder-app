using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public abstract class RecurrencePolicy : ValueObject
{
    public abstract bool RequiresNextDueOverride { get; }

    public abstract DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions);

    public abstract bool IsTerminal(IReadOnlyList<DateOnly> completions);

    public abstract RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride);
}
