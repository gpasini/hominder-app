using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hominder.Infrastructure.Persistence;

public sealed class HominderDbContextFactory : IDesignTimeDbContextFactory<HominderDbContext>
{
    public HominderDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HominderDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=hominder;Username=hominder;Password=hominder")
            .Options;

        return new HominderDbContext(options, new NoOpPublisher());
    }

    private sealed class NoOpPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
