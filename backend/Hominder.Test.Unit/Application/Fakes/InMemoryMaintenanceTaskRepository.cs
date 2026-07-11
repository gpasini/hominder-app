using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Application.Fakes;

public sealed class InMemoryMaintenanceTaskRepository : IMaintenanceTaskRepository
{
    public List<MaintenanceTask> Items { get; } = [];

    public Task AddAsync(MaintenanceTask task, CancellationToken cancellationToken = default)
    {
        Items.Add(task);
        return Task.CompletedTask;
    }

    public Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(task => task.Id == id));

    public Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceTask>>(Items);

    public void Remove(MaintenanceTask task) => Items.Remove(task);
}
