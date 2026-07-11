using Hominder.Domain.Maintenance;

namespace Hominder.Application.Common.Persistence;

public interface IMaintenanceTaskRepository
{
    Task AddAsync(MaintenanceTask task, CancellationToken cancellationToken = default);

    Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default);

    void Remove(MaintenanceTask task);
}
