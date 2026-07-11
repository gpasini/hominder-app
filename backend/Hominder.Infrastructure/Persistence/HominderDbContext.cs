using Hominder.Application.Common.Persistence;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence;

public sealed class HominderDbContext : DbContext, IUnitOfWork
{
    public HominderDbContext(DbContextOptions<HominderDbContext> options)
        : base(options)
    {
    }

    public DbSet<MaintenanceTask> MaintenanceTasks => Set<MaintenanceTask>();

    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await operation();
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HominderDbContext).Assembly);
}
