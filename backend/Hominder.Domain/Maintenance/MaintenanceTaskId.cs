namespace Hominder.Domain.Maintenance;

public readonly record struct MaintenanceTaskId(Guid Value)
{
    public static MaintenanceTaskId New() => new(Guid.NewGuid());
}
