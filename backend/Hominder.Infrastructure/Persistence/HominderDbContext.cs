using Hominder.Application.Common.Persistence;
using Hominder.Domain.Common;
using Hominder.Domain.Household;
using Hominder.Domain.Maintenance;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hominder.Infrastructure.Persistence;

public sealed class HominderDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;

    public HominderDbContext(DbContextOptions<HominderDbContext> options, IPublisher publisher)
        : base(options) => _publisher = publisher;

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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HominderDbContext).Assembly);

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var aggregates = ChangeTracker.Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            if (aggregates.Count == 0)
            {
                return;
            }

            var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
            aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
