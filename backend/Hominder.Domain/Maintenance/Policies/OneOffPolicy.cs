namespace Hominder.Domain.Maintenance.Policies;

public sealed class OneOffPolicy : RecurrencePolicy
{
    public OneOffPolicy(DateOnly dueDate) => DueDate = dueDate;

    public DateOnly DueDate { get; }

    public override bool RequiresNextDueOverride => false;

    public override DueWindow NextDueWindow(DateOnly today, IReadOnlyList<DateOnly> completions) =>
        new(DueDate, DueDate);

    public override bool IsTerminal(IReadOnlyList<DateOnly> completions) => completions.Count > 0;

    public override RecurrencePolicy WithCompletion(DateOnly completedOn, DateOnly? nextDueOverride) => this;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DueDate;
    }
}
