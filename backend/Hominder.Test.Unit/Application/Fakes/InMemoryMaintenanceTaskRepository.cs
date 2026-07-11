using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;

namespace Hominder.Test.Unit.Application.Fakes;

public sealed class InMemoryMaintenanceTaskRepository : IMaintenanceTaskRepository
{
    public List<MaintenanceTask> Items { get; } = [];

    public Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(task => task.Id == id));

    public Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceTask>>(Items);

    public void Save(MaintenanceTask task)
    {
        if (!Items.Contains(task))
        {
            Items.Add(task);
        }
    }

    public void Remove(MaintenanceTask task) => Items.Remove(task);
}
