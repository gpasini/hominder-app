namespace Hominder.Domain.Maintenance;

public sealed record MaintenanceEvaluation(MaintenanceStatus Status, DueWindow Window, int DaysOverdue);
