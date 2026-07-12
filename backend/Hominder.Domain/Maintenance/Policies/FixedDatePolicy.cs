using Hominder.Domain.Common;

namespace Hominder.Domain.Maintenance.Policies;

public sealed class FixedDatePolicy : RecurrencePolicy
{
    public FixedDatePolicy(DateOnly dueDate) => DueDate = dueDate;

    public DateOnly DueDate { get; }

    public override bool RequiresNextDueOverride => true;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions) =>
        new(DueDate, DueDate);

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => false;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride)
    {
        if (nextDueOverride is null)
        {
            throw new DomainException("Une échéance à date fixe exige la prochaine date à la complétion.");
        }

        return new FixedDatePolicy(nextDueOverride.Value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DueDate;
    }
}
