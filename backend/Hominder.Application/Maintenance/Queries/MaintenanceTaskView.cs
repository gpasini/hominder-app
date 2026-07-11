namespace Hominder.Application.Maintenance.Queries;

public sealed record MaintenanceTaskView(
    Guid Id,
    string Title,
    string? Notes,
    string Status,
    DateOnly OpenDate,
    DateOnly DueDate,
    int DaysOverdue,
    Guid? AssigneeId,
    string? AssigneeName,
    bool RequiresNextDueOverride,
    RecurrencePolicyInput Policy);
