using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceTaskRepository : IMaintenanceTaskRepository
{
    private readonly HominderDbContext _context;

    public MaintenanceTaskRepository(HominderDbContext context) => _context = context;

    public Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default) =>
        _context.MaintenanceTasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.MaintenanceTasks.ToListAsync(cancellationToken);

    public void Save(MaintenanceTask task)
    {
        if (_context.Entry(task).State == EntityState.Detached)
        {
            _context.MaintenanceTasks.Add(task);
        }
    }

    public void Remove(MaintenanceTask task) => _context.MaintenanceTasks.Remove(task);
}
