using Hominder.Domain.Maintenance;

namespace Hominder.Application.Common.Persistence;

public interface IMaintenanceTaskRepository
{
    Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default);

    void Save(MaintenanceTask task);

    void Remove(MaintenanceTask task);
}
