using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance.Policies;

namespace Hominder.Domain.Maintenance;

public sealed class MaintenanceTask : AggregateRoot<MaintenanceTaskId>
{
    private readonly List<Completion> _completions = [];

    private MaintenanceTask(
        MaintenanceTaskId id,
        string title,
        string? notes,
        RecurrencePolicy policy,
        HouseholdMemberId? assigneeId)
        : base(id)
    {
        Title = title;
        Notes = notes;
        Policy = policy;
        AssigneeId = assigneeId;
    }

    public string Title { get; private set; }

    public string? Notes { get; private set; }

    public RecurrencePolicy Policy { get; private set; }

    public HouseholdMemberId? AssigneeId { get; private set; }

    public IReadOnlyList<Completion> Completions => _completions.AsReadOnly();

    public static MaintenanceTask Create(
        string title,
        string? notes,
        RecurrencePolicy policy,
        HouseholdMemberId? assigneeId)
    {
        var cleanTitle = RequireTitle(title);
        return new MaintenanceTask(MaintenanceTaskId.New(), cleanTitle, notes, policy, assigneeId);
    }

    public void Update(string title, string? notes, RecurrencePolicy policy, HouseholdMemberId? assigneeId)
    {
        Title = RequireTitle(title);
        Notes = notes;
        Policy = policy;
        AssigneeId = assigneeId;
    }

    public void MarkDone(DateOnly completedOn, HouseholdMemberId completedBy, DateOnly? nextDueOverride)
    {
        var completedDates = CompletedDates();

        if (Policy.IsTerminal(completedDates))
        {
            throw new DomainException("Cette tâche ponctuelle est déjà terminée.");
        }

        if (Policy.RequiresNextDueOverride && nextDueOverride is null)
        {
            throw new DomainException("Cette tâche exige la prochaine échéance à la complétion.");
        }

        if (!Policy.RequiresNextDueOverride && nextDueOverride is not null)
        {
            throw new DomainException("Cette tâche ne permet pas de saisir la prochaine échéance.");
        }

        _completions.Add(new Completion(completedOn, completedBy));
        Policy = Policy.WithCompletion(completedOn, nextDueOverride);

        RaiseDomainEvent(new MaintenanceTaskCompletedDomainEvent(Id, completedOn, completedBy, DateTime.UtcNow));
    }

    public MaintenanceEvaluation Evaluate(DateOnly today)
    {
        var completedDates = CompletedDates();
        var window = Policy.NextDueWindow(today, completedDates);

        if (Policy.IsTerminal(completedDates))
        {
            return new MaintenanceEvaluation(MaintenanceStatus.Done, window, 0);
        }

        var status = today < window.OpenDate
            ? MaintenanceStatus.Upcoming
            : today <= window.DueDate
                ? MaintenanceStatus.Due
                : MaintenanceStatus.Overdue;

        var daysOverdue = status == MaintenanceStatus.Overdue
            ? today.DayNumber - window.DueDate.DayNumber
            : 0;

        return new MaintenanceEvaluation(status, window, daysOverdue);
    }

    private IReadOnlyList<DateOnly> CompletedDates() =>
        _completions.Select(completion => completion.CompletedOn).ToList();

    private static string RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Le titre est obligatoire.");
        }

        return title.Trim();
    }
}
