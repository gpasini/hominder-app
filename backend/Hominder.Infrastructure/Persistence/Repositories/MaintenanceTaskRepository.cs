using Hominder.Application.Common.Persistence;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceTaskRepository : IMaintenanceTaskRepository
{
    private readonly HominderDbContext _context;

    public MaintenanceTaskRepository(HominderDbContext context) => _context = context;

    public async Task AddAsync(MaintenanceTask task, CancellationToken cancellationToken = default) =>
        await _context.MaintenanceTasks.AddAsync(task, cancellationToken);

    public Task<MaintenanceTask?> GetByIdAsync(MaintenanceTaskId id, CancellationToken cancellationToken = default) =>
        _context.MaintenanceTasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceTask>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.MaintenanceTasks.ToListAsync(cancellationToken);

    public void Remove(MaintenanceTask task) => _context.MaintenanceTasks.Remove(task);
}
